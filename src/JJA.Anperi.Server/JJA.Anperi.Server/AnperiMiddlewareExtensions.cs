using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;

namespace JJA.Anperi.Server
{
    public static class AnperiMiddlewareExtensions
    {
        public static void UseAnperiWebSocket(this IApplicationBuilder app, string path, int wsBufferSize, CancellationToken appStopping)
        {
            AnperiWebSocketManager.Instance.Path = path;
            AnperiWebSocketManager.Instance.WsBufferSize = wsBufferSize;
            AnperiWebSocketManager.Instance.RequestCancelToken = appStopping;
            app.Use(AnperiWebSocketManager.Instance.Middleware);
        }
    }
}
