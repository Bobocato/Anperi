using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Api;
using JJA.Anperi.Api.Shared;
using JJA.Anperi.Server.Model;
using JJA.Anperi.Server.Utility;
using Microsoft.AspNetCore.Http;

namespace JJA.Anperi.Server
{
    public class AuthenticatedWebSocketConnection
    {
        private readonly HttpContext _context;
        private readonly WebSocket _socket;
        private readonly byte[] _buffer;
        private readonly RegisteredDevice _device;

        public AuthenticatedWebSocketConnection(HttpContext context, WebSocket socket, byte[] buffer, RegisteredDevice device, AnperiDbContext dbContext)
        {
            _context = context;
            _socket = socket;
            _buffer = buffer;
            _device = device;
        }

        public async Task<WebSocketCloseStatus> Run(CancellationToken token)
        {
            WebSocketApiResult apiObjectResult =
                await _socket.ReceiveApiMessage(new ArraySegment<byte>(_buffer), token);
            while (!apiObjectResult.SocketResult.CloseStatus.HasValue && !token.IsCancellationRequested)
            {
                if (apiObjectResult.Obj == null)
                {
                    await _socket.SendJson(
                        SharedJsonApiObjectFactory.CreateError(apiObjectResult.JsonException.Message), token);
                }
                else
                {
                    await HandleApiMessage(apiObjectResult.Obj, token);
                }

                apiObjectResult = await _socket.ReceiveApiMessage(new ArraySegment<byte>(_buffer), token);
            }
            return apiObjectResult.SocketResult.CloseStatus.HasValue ? apiObjectResult.SocketResult.CloseStatus.Value : WebSocketCloseStatus.NormalClosure;
        }

        private async Task HandleApiMessage(JsonApiObject message, CancellationToken token)
        {
            await _socket.SendJson(message, token);
        }
    }
}