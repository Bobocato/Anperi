using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BCrypt.Net;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.Server.Model;
using JJA.Anperi.Server.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace JJA.Anperi.Server
{
    public class AnperiWebSocketMiddleware
    {
        private readonly ILogger<AnperiWebSocketMiddleware> _logger;
        private readonly RequestDelegate _next;
        private readonly IOptions<Options> _options;
        private readonly object _syncRootActiveConnections = new object();
        private readonly List<AuthenticatedWebSocketConnection> _activeConnections;

        public AnperiWebSocketMiddleware(RequestDelegate next, ILogger<AnperiWebSocketMiddleware> logger, IOptions<Options> options)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _activeConnections = new List<AuthenticatedWebSocketConnection>();
        }

        public class Options
        {
            public string Path { get; set; } = "/api/ws";
            public int WsBufferSize { get; set; } = 16 * 1024;
            public CancellationToken RequestCancelToken { get; set; } = CancellationToken.None;
        }

        public async Task InvokeAsync(HttpContext ctx, AnperiDbContext dbContext)
        {
            if (ctx.Request.Path != _options.Value.Path)
            {
                await _next(ctx);
                return;
            }
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            try
            {
                await HandleWebSocket(ctx, dbContext);
            }
            catch (WebSocketException se)
            {
                if (!(se.InnerException is BadHttpRequestException)) throw;
                _logger.LogWarning($"BadHttpRequestException occured while handling a websocket: {se.Message} -> {se.InnerException.Message}");
            }
        }

        private async Task HandleWebSocket(HttpContext ctx, AnperiDbContext dbContext)
        {
            WebSocket socket = await ctx.WebSockets.AcceptWebSocketAsync();
            var buffer = new byte[_options.Value.WsBufferSize];

            WebSocketApiResult apiObjectResult =
                await socket.ReceiveApiMessage(new ArraySegment<byte>(buffer), _options.Value.RequestCancelToken);
            bool authFailed = true;
            WebSocketCloseStatus closeStatus = WebSocketCloseStatus.Empty;
            if (apiObjectResult.Obj == null)
            {
                await socket.SendJson(
                    SharedJsonApiObjectFactory.CreateError(apiObjectResult.JsonException.Message),
                    _options.Value.RequestCancelToken);
            }
            else
            {
                if (apiObjectResult.Obj.context == JsonApiContextTypes.server &&
                    apiObjectResult.Obj.message_type == JsonApiMessageTypes.request)
                {
                    SharedJsonDeviceType type;
                    try
                    {
                        type = Enum.Parse<SharedJsonDeviceType>(apiObjectResult.Obj.data[nameof(JsonLoginData.device_type)]);
                    }
                    catch (Exception)
                    {
                        await socket.SendJson(SharedJsonApiObjectFactory.CreateError("device_type not valid"), _options.Value.RequestCancelToken);
                        return;
                    }
                    if (apiObjectResult.Obj.message_code == SharedJsonRequestCode.login.ToString())
                    {
                        string token = null;
                        try
                        {
                            token = apiObjectResult.Obj.data[nameof(JsonLoginData.token)];
                        }
                        catch (Exception ex)
                        {
                            await socket.SendJson(
                                SharedJsonApiObjectFactory.CreateError($"Error retrieving token from request."));
                            _logger.LogError(ex, "Error retrieving token from request");
                        }
                        if (!string.IsNullOrEmpty(token))
                        {
                            RegisteredDevice device = dbContext.RegisteredDevices.SingleOrDefault(d => d.Token == token);
                            if (device != null)
                            {
                                closeStatus = await LoginDevice(ctx, socket, buffer, device, dbContext);
                                authFailed = false;
                            }
                        }
                    }
                    else if (apiObjectResult.Obj.message_code == SharedJsonRequestCode.register.ToString())
                    {
                        RegisteredDevice device;
                        switch (type)
                        {
                            case SharedJsonDeviceType.host:
                                device = new Host();
                                break;
                            case SharedJsonDeviceType.peripheral:
                                device = new Peripheral();
                                break;
                            default:
                                throw new NotImplementedException();
                        }
                        device.Token = Cryptography.CreateAuthToken();
                        if (apiObjectResult.Obj.data.TryGetValue("name", out dynamic name))
                        {
                            device.Name = name;
                        }
                        else
                        {
                            device.Name = "GIVE ME A NAME PLEASE";
                            await socket.SendJson(SharedJsonApiObjectFactory.CreateError("A device registration requires a name!"));
                        }
                        dbContext.RegisteredDevices.Add(device);
                        await dbContext.SaveChangesAsync();
                        await socket.SendJson(SharedJsonApiObjectFactory.CreateRegisterResponse(device.Token, device.Name));
                        closeStatus = await LoginDevice(ctx, socket, buffer, device, dbContext);
                        authFailed = false;
                    }
                    else
                    {
                        await socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError(
                                "The first message is required to be a login or register request!"));
                    }
                }
            }

            if (authFailed)
            {
                await socket.SendJson(SharedJsonApiObjectFactory.CreateLoginResponse(false, null));
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication failed.", cts.Token);
            }
            else if (_options.Value.RequestCancelToken.IsCancellationRequested)
            {
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server is shutting down.", cts.Token);
            }
            else
            {
                await socket.CloseAsync(closeStatus, apiObjectResult.SocketResult.CloseStatusDescription, CancellationToken.None);
            }
        }

        private async Task<WebSocketCloseStatus> LoginDevice(HttpContext context, WebSocket socket, byte[] buffer, RegisteredDevice device, AnperiDbContext dbContext)
        {
            await socket.SendJson(SharedJsonApiObjectFactory.CreateLoginResponse(true, device.Name));
            var connection = new AuthenticatedWebSocketConnection(context, socket, buffer, device, dbContext, _logger, this);
            lock (_syncRootActiveConnections)
            {
                _activeConnections.Add(connection);
            }
            if (connection.IsPeripheral) OnPeripheralLoggedIn(connection, device as Peripheral);
            WebSocketCloseStatus closeStatus = await connection.Run(_options.Value.RequestCancelToken);
            if (connection.IsPeripheral) OnPeripheralLoggedOut(connection, device as Peripheral);
            lock (_syncRootActiveConnections)
            {
                _activeConnections.Remove(connection);
            }
            return closeStatus;
        }

        internal AuthenticatedWebSocketConnection GetConnectionForId(int id)
        {
            lock (_activeConnections)
            {
                return _activeConnections.SingleOrDefault(c => c.Device.Id == id);
            }
        }

        private void OnPeripheralLoggedIn(AuthenticatedWebSocketConnection c, Peripheral peripheral)
        {
            lock (_activeConnections)
            {
                peripheral.PairedHosts.ForEach(hp =>
                {
                    _activeConnections.ForEach(ac =>
                    {
                        if (hp.HostId == ac.Device.Id)
                            Task.Run(() =>
                            {
                                PeripheralLoggedIn?.Invoke(this, new AuthenticatedWebSocketEventArgs(c));
                            });
                    });
                });
            }
        }
        public event EventHandler<AuthenticatedWebSocketEventArgs> PeripheralLoggedIn;

        private void OnPeripheralLoggedOut(AuthenticatedWebSocketConnection c, Peripheral peripheral)
        {
            lock (_activeConnections)
            {
                peripheral.PairedHosts.ForEach(hp =>
                {
                    _activeConnections.ForEach(ac =>
                    {
                        if (hp.HostId == ac.Device.Id)
                            Task.Run(() =>
                            {
                                PeripheralLoggedOut?.Invoke(this, new AuthenticatedWebSocketEventArgs(c));
                            });
                    });
                });
            }
        }
        public event EventHandler<AuthenticatedWebSocketEventArgs> PeripheralLoggedOut;
    }

    public class AuthenticatedWebSocketEventArgs
    {
        public AuthenticatedWebSocketEventArgs(AuthenticatedWebSocketConnection connection)
        {
            Connection = connection;
        }

        public AuthenticatedWebSocketConnection Connection { get; set; }
    }
}