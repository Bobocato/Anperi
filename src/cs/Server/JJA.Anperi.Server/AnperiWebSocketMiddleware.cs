using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Internal.Api;
using JJA.Anperi.Internal.Api.Shared;
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
        private readonly Dictionary<int, AuthenticatedWebSocketConnection> _activeConnections;

        public AnperiWebSocketMiddleware(RequestDelegate next, ILogger<AnperiWebSocketMiddleware> logger, IOptions<Options> options)
        {
            _next = next;
            _logger = logger;
            _options = options;
            _activeConnections = new Dictionary<int, AuthenticatedWebSocketConnection>();
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
                                            _activeConnections.TryGetValue(device.Id, out activeConn);
                                        }
                                        if (activeConn == null)
                                        {
                                            closeStatus = await LoginDevice(ctx, socket, buffer, device, dbContext);
                                            authFailed = false;
                                        }
                                        else
                                        {
                                            await socket.SendJson(SharedJsonApiObjectFactory.CreateError(
                                                "Error logging in ... a device with your token is already active."));
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
            List<AuthenticatedWebSocketConnection> connectedPairedDevices = await GetOnlinePairedDevices(device, dbContext);
            var connection = new AuthenticatedWebSocketConnection(context, socket, buffer, device, dbContext, _logger, this, connectedPairedDevices);
            lock (_syncRootActiveConnections)
            {
                _activeConnections.Add(connection.Device.Id, connection);
            }
            WebSocketCloseStatus closeStatus;
            try
            { 
                await OnDeviceLoggedIn(connection, dbContext);
                closeStatus = await connection.Run(_options.Value.RequestCancelToken);
            }
            catch (WebSocketException se)
            {
                if (!(se.InnerException is BadHttpRequestException)) throw;
                closeStatus = WebSocketCloseStatus.Empty;
                _logger.LogWarning(
                    $"BadHttpRequestException occured while handling a websocket: {se.Message} -> {se.InnerException.Message}");
            }
            finally
            {
                lock (_syncRootActiveConnections)
                {
                    _activeConnections.Remove(connection.Device.Id);
                }
                await OnDeviceLoggedOut(connection, dbContext);
            }
            lock (_syncRootActiveConnections)
            {
                _activeConnections.Remove(connection.Device.Id);
            }
            return closeStatus;
        }

        private async Task<IEnumerable<int>> GetPairedDeviceIds(RegisteredDevice device, AnperiDbContext dbContext)
        {
            return await Task.Run(() =>
            {
                IEnumerable<int> pairedDeviceIds;
                if (device is Host)
                {
                    pairedDeviceIds = dbContext.HostPeripherals.Where(hp => hp.HostId == device.Id)
                        .Select(hp => hp.PeripheralId);
                }
                else if (device is Peripheral)
                {
                    pairedDeviceIds = dbContext.HostPeripherals.Where(hp => hp.PeripheralId == device.Id)
                        .Select(hp => hp.HostId);
                }
                else
                    throw new NotImplementedException(
                        $"The device type {device.GetType().AssemblyQualifiedName} is not implemented in GetPairedDeviceIds");
                return pairedDeviceIds.ToList();
            });
        }

        private async Task<List<AuthenticatedWebSocketConnection>> GetOnlinePairedDevices(
            RegisteredDevice device, AnperiDbContext dbContext)
        {
            IEnumerable<int> pairedDeviceIds = await GetPairedDeviceIds(device, dbContext);

            lock (_activeConnections)
            {
                return _activeConnections.Where(c => pairedDeviceIds.Contains(c.Key)).Select(c => c.Value).ToList();
            }
        }

        private async Task OnDeviceLoggedOut(AuthenticatedWebSocketConnection connection, AnperiDbContext dbContext)
        {
            IEnumerable<int> pairedDeviceIds = await GetPairedDeviceIds(connection.Device, dbContext);
            
            lock (_activeConnections)
            {
                foreach (int pairedDeviceId in pairedDeviceIds)
                {
                    _activeConnections.TryGetValue(pairedDeviceId, out var c);
                    c?.OnPairedDeviceLogoff(this, new AuthenticatedWebSocketEventArgs(connection));
                }
            }
        }

        private async Task OnDeviceLoggedIn(AuthenticatedWebSocketConnection connection, AnperiDbContext dbContext)
        {
            IEnumerable<int> pairedDeviceIds = await GetPairedDeviceIds(connection.Device, dbContext);

            lock (_activeConnections)
            {
                foreach (int pairedDeviceId in pairedDeviceIds)
                {
                    _activeConnections.TryGetValue(pairedDeviceId, out var c);
                    c?.OnPairedDeviceLogin(this, new AuthenticatedWebSocketEventArgs(connection));
                }
            }
        }

        internal AuthenticatedWebSocketConnection GetConnectionForId(int id)
        {
            lock (_activeConnections)
            {
                _activeConnections.TryGetValue(id, out var val);
                return val;
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