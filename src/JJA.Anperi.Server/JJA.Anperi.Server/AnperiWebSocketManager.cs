using System;
using System.Collections.Generic;
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
using Newtonsoft.Json;

namespace JJA.Anperi.Server
{
    public class AnperiWebSocketManager
    {
        // ReSharper disable once InconsistentNaming
        private static readonly Lazy<AnperiWebSocketManager> _instance =
            new Lazy<AnperiWebSocketManager>(() => new AnperiWebSocketManager());

        public static AnperiWebSocketManager Instance => _instance.Value;

        public string Path { get; set; } = "/ws";
        public int WsBufferSize { get; set; }
        public AnperiDbContext DbContext { get; set; }
        public CancellationToken RequestCancelToken { get; set; } = CancellationToken.None;

        internal async Task Middleware(HttpContext ctx, Func<Task> next)
        {
            if (ctx.Request.Path != Path)
            {
                await next();
                return;
            }
            if (!ctx.WebSockets.IsWebSocketRequest)
            {
                ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }
            await HandleWebSocket(ctx);
        }

        private async Task HandleWebSocket(HttpContext ctx)
        {
            WebSocket socket = await ctx.WebSockets.AcceptWebSocketAsync();
            var buffer = new byte[WsBufferSize];

            WebSocketApiResult apiObjectResult =
                await socket.ReceiveApiMessage(new ArraySegment<byte>(buffer), RequestCancelToken);
            bool authFailed = true;
            if (apiObjectResult.Obj == null)
            {
                await socket.SendJson(
                    SharedJsonApiObjectFactory.CreateError(apiObjectResult.JsonException.Message),
                    RequestCancelToken);
            }
            else
            {
                if (apiObjectResult.Obj.context == JsonApiContextTypes.server &&
                    apiObjectResult.Obj.message_type == JsonApiMessageTypes.request)
                {
                    if (apiObjectResult.Obj.message_code == SharedJsonRequestCode.login.ToString())
                    {
                        authFailed = !await LoginDevice(ctx, socket, buffer);
                    }
                    else if (apiObjectResult.Obj.message_code == SharedJsonRequestCode.register.ToString())
                    {
                        //TODO: register
                        authFailed = !await LoginDevice(ctx, socket, buffer);
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
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                await socket.CloseAsync(WebSocketCloseStatus.PolicyViolation, "Authentication failed.", cts.Token);
            }
            else if (RequestCancelToken.IsCancellationRequested)
            {
                CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(1000));
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Server is shutting down.", cts.Token);
            }
            else
            {
                await socket.CloseAsync(apiObjectResult.SocketResult.CloseStatus.Value,
                    apiObjectResult.SocketResult.CloseStatusDescription, CancellationToken.None);
            }
        }

        private async Task<bool> LoginDevice(HttpContext context, WebSocket socket, byte[] buffer)
        {
            //TODO: do
            var connection = new ActiveWebSocketConnection(context, socket, buffer, null);
            await connection.Run(RequestCancelToken);

            return true;
        }
    }
}