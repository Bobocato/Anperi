using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Api;
using JJA.Anperi.Server.Model;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;

namespace JJA.Anperi.Server.Utility
{
    class WebSocketStringResult
    {
        public WebSocketReceiveResult SocketResult { get; set; }
        public string Message { get; set; }
    }

    class WebSocketApiResult
    {
        public WebSocketReceiveResult SocketResult { get; set; }
        public JsonApiObject Obj { get; set; }
        public JsonException JsonException { get; set; }
    }

    static class Extensions
    {
        internal static async Task SendString(this WebSocket socket, string message, CancellationToken? cancellationToken = null, Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.UTF8;
            if (cancellationToken == null) cancellationToken = CancellationToken.None;

            await socket.SendAsync(new ArraySegment<byte>(encoding.GetBytes(message)),WebSocketMessageType.Text, true, cancellationToken.Value);
        }

        internal static async Task SendJson(this WebSocket socket, JsonApiObject obj, CancellationToken? cancellationToken = null, Encoding encoding = null)
        {
            await SendString(socket, obj.Serialize(), cancellationToken, encoding);
        }

        internal static async Task<WebSocketStringResult> ReceiveString(this WebSocket socket, ArraySegment<byte> buffer, CancellationToken? cancellationToken = null)
        {
            if (cancellationToken == null) cancellationToken = CancellationToken.None;
            WebSocketReceiveResult wsResult = await socket.ReceiveAsync(buffer, cancellationToken.Value);
            string message = null;
            if (wsResult.MessageType == WebSocketMessageType.Text)
            {
                message = Encoding.UTF8.GetString(buffer.Array, 0, wsResult.Count);
            }
            return new WebSocketStringResult { Message = message, SocketResult = wsResult };
        }

        internal static async Task<WebSocketApiResult> ReceiveApiMessage(this WebSocket socket, ArraySegment<byte> buffer, CancellationToken? cancellationToken = null)
        {
            WebSocketStringResult res = await socket.ReceiveString(buffer, cancellationToken);
            var result = new WebSocketApiResult {SocketResult = res.SocketResult};
            if (res.SocketResult.MessageType != WebSocketMessageType.Text)
            {
                result.JsonException = new JsonException("Can't read binary messages as JSON.");
            }
            else if (res.Message == "")
            {
                result.JsonException = new JsonException("The message was empty.");
            }
            else if (res.Message != null)
            {
                try
                {
                    result.Obj = JsonApiObject.Deserialize(res.Message);
                }
                catch (JsonException ex)
                {
                    result.JsonException = ex;
                }
            }
            return result;
        }

        /// <summary>
        /// TryGetValue with typecheck for dynamic dictionary.
        /// </summary>
        /// <returns>if the key existed and the dynamic was of the given type</returns>
        internal static bool TryGetValue<TK, T>(this Dictionary<TK, dynamic> dict, TK key, out T val)
        {
            bool result = false;
            if (dict.TryGetValue(key, out dynamic dyn) && dyn is T)
            {
                val = dyn;
                result = true;
            }
            else
            {
                val = default(T);
            }
            return result;
        }
    }
}
