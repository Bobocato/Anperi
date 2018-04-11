using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Api;
using JJA.Anperi.Api.SharedMessages;
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

            WebSocketApiResult apiObjectResult = await socket.ReceiveApiMessage(new ArraySegment<byte>(buffer), RequestCancelToken);
            while (!apiObjectResult.SocketResult.CloseStatus.HasValue && !RequestCancelToken.IsCancellationRequested)
            {
                if (apiObjectResult.Obj == null)
                {
                    if (apiObjectResult.JsonException == null)
                    {
                        await socket.SendJson(SharedMessageJsonApiObjectFactory.CreateError(
                            "The sent message was binary or not valid (probably an empty string)"), RequestCancelToken);
                    }
                    else
                    {
                        await socket.SendJson(
                            SharedMessageJsonApiObjectFactory.CreateError(
                                $"Error parsing JSON: {apiObjectResult.JsonException.Message}"), RequestCancelToken);
                    }
                }
                else
                {
                    await socket.SendString($"This is what I deserialized: {apiObjectResult.Obj.Serialize()}", RequestCancelToken);
                }

                apiObjectResult = await socket.ReceiveApiMessage(new ArraySegment<byte>(buffer), RequestCancelToken);
            }
            if (RequestCancelToken.IsCancellationRequested)
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
    }
}