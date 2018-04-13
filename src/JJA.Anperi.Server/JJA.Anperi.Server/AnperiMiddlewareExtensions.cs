using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace JJA.Anperi.Server
{
    public static class AnperiMiddlewareExtensions
    {
        public static void UseAnperiWebSocket(this IApplicationBuilder app, string path, int wsBufferSize, IServiceProvider serviceProvider, CancellationToken appStopping)
        {
            AnperiWebSocketManager.Instance.Path = path;
            AnperiWebSocketManager.Instance.WsBufferSize = wsBufferSize;
            AnperiWebSocketManager.Instance.RequestCancelToken = appStopping;
            AnperiWebSocketManager.Instance.DbContext = serviceProvider.GetService<AnperiDbContext>();
            app.Use(AnperiWebSocketManager.Instance.Middleware);
        }
    }
}
