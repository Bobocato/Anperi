using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.Server.Model;
using JJA.Anperi.Server.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

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
                _logger.LogWarning(
                    $"BadHttpRequestException occured while handling a websocket: {se.Message} -> {se.InnerException.Message}");
            }
            finally
            {
                lock (_syncRootActiveConnections)
                {
                    var connection =
                        _activeConnections.SingleOrDefault(c => c.Context.Connection.Id == ctx.Connection.Id);
                    if (connection != null)
                    {
                        if (_activeConnections.Remove(connection))
                            OnDeviceLoggedOut(connection, connection.Device);
                    }
                }
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
                    apiObjectResult.Obj.data.TryGetValue(nameof(JsonLoginData.device_type), out string typeString);
                    if (Enum.TryParse(typeString, out SharedJsonDeviceType type) &&
                        Enum.TryParse(apiObjectResult.Obj.message_code, out SharedJsonRequestCode code))
                    {
                        RegisteredDevice device;
                        switch (code)
                        {
                            case SharedJsonRequestCode.login:
                                if (!apiObjectResult.Obj.data.TryGetValue("token", out string token))
                                {
                                    await socket.SendJson(
                                        SharedJsonApiObjectFactory.CreateError("Error retrieving token from request."));
                                }
                                else
                                {
                                    switch (type)
                                    {
                                        case SharedJsonDeviceType.host:
                                            device = dbContext.Hosts.SingleOrDefault(d => d.Token == token);
                                            break;
                                        case SharedJsonDeviceType.peripheral:
                                            device = dbContext.Peripherals.SingleOrDefault(d => d.Token == token);
                                            break;
                                        default:
                                            throw new NotImplementedException();
                                    }
                                    if (device != null)
                                    {
                                        AuthenticatedWebSocketConnection activeConn;
                                        lock (_activeConnections)
                                        {
                                            activeConn = _activeConnections.SingleOrDefault(c => c.Device.Id == device.Id);
                                        }
                                        if (activeConn != null)
                                        {
                                            closeStatus = await LoginDevice(ctx, socket, buffer, device, dbContext);
                                            authFailed = false;
                                        }
                                    }
                                }
                                break;
                            case SharedJsonRequestCode.register:
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
                                if (apiObjectResult.Obj.data.TryGetValue("name", out string name))
                                {
                                    device.Name = name;
                                }
                                else
                                {
                                    device.Name = "GIVE ME A NAME PLEASE";
                                    await socket.SendJson(
                                        SharedJsonApiObjectFactory.CreateError(
                                            "A device registration requires a name!"));
                                }
                                dbContext.RegisteredDevices.Add(device);
                                await dbContext.SaveChangesAsync();
                                await socket.SendJson(
                                    SharedJsonApiObjectFactory.CreateRegisterResponse(device.Token, device.Name));
                                closeStatus = await LoginDevice(ctx, socket, buffer, device, dbContext);
                                authFailed = false;
                                break;
                            default:
                                await socket.SendJson(
                                    SharedJsonApiObjectFactory.CreateError("Only login and register are valid here."));
                                break;
                        }
                    }
                    else
                    {
                        await socket.SendJson(
                            SharedJsonApiObjectFactory.CreateError(
                                "Error parsing correct login or register parameters."));
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
            OnDeviceLoggedIn(connection, device);
            WebSocketCloseStatus closeStatus = await connection.Run(_options.Value.RequestCancelToken);
            OnDeviceLoggedOut(connection, device);
            lock (_syncRootActiveConnections)
            {
                _activeConnections.Remove(connection);
            }
            return closeStatus;
        }

        private void OnDeviceLoggedOut(AuthenticatedWebSocketConnection connection, RegisteredDevice device)
        {
            lock (_activeConnections)
            {
                //TODO: implement
            }
        }

        private void OnDeviceLoggedIn(AuthenticatedWebSocketConnection connection, RegisteredDevice device)
        {
            lock (_activeConnections)
            {
                //TODO: implement
            }
        }

        internal AuthenticatedWebSocketConnection GetConnectionForId(int id)
        {
            lock (_activeConnections)
            {
                return _activeConnections.SingleOrDefault(c => c.Device.Id == id);
            }
        }
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