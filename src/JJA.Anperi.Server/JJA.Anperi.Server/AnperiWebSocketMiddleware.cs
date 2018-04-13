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

        public AnperiWebSocketMiddleware(RequestDelegate next, ILogger<AnperiWebSocketMiddleware> logger, IOptions<Options> options)
        {
            _next = next;
            _logger = logger;
            _options = options;
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
            await HandleWebSocket(ctx, dbContext);
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
                        dbContext.RegisteredDevices.Add(device);
                        await dbContext.SaveChangesAsync();
                        await socket.SendJson(SharedJsonApiObjectFactory.CreateRegisterResponse(device.Token));
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
                await socket.SendJson(SharedJsonApiObjectFactory.CreateLoginResponse(false));
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
            await socket.SendJson(SharedJsonApiObjectFactory.CreateLoginResponse(true));
            var connection = new AuthenticatedWebSocketConnection(context, socket, buffer, device, dbContext);
            return await connection.Run(_options.Value.RequestCancelToken);
        }
    }
}