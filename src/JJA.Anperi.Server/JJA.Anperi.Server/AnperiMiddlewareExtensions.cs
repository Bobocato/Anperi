using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace JJA.Anperi.Server
{
    public static class AnperiMiddlewareExtensions
    {
        public static void UseAnperiWebSocket(this IApplicationBuilder app, AnperiWebSocketMiddleware.Options options = null)
        {
            if (options == null) options = new AnperiWebSocketMiddleware.Options();
            app.UseMiddleware<AnperiWebSocketMiddleware>(Options.Create(options));
        }
    }
}
