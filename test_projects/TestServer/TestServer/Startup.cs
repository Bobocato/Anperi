using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace TestServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            int webSocketReceiveBufferSize = int.Parse(Configuration["ServerStartupSettings:WebSocketReceiveBufferSize"]);

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseMvc();
            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ReceiveBufferSize = webSocketReceiveBufferSize
            });

            //TODO: find a proper place for logic, probably another class
            //copied from https://docs.microsoft.com/en-us/aspnet/core/fundamentals/websockets
            app.Use(async (ctx, next) =>
            {
                if (ctx.Request.Path == "/ws")
                {
                    if (ctx.WebSockets.IsWebSocketRequest)
                    {
                        WebSocket socket = await ctx.WebSockets.AcceptWebSocketAsync();
                        var buffer = new byte[webSocketReceiveBufferSize];
                        WebSocketReceiveResult wrr =
                            await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        while (!wrr.CloseStatus.HasValue)
                        {
                            if (wrr.MessageType == WebSocketMessageType.Binary)
                            {
                                await socket.SendAsync(
                                    new ArraySegment<byte>(Encoding.UTF8.GetBytes(
                                        "OMEGALUL, this wasn't something I want to repeat :( (don't send bytes)")),
                                    WebSocketMessageType.Text, true, CancellationToken.None);
                            }
                            else
                            {
                                await socket.SendAsync(new ArraySegment<byte>(buffer, 0, wrr.Count), wrr.MessageType,
                                    wrr.EndOfMessage, CancellationToken.None);
                            }
                            wrr = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);
                        }
                        await socket.CloseAsync(wrr.CloseStatus.Value, wrr.CloseStatusDescription,
                            CancellationToken.None);
                    }
                    else
                    {
                        ctx.Response.Headers.Add("SomeTest", "very important debug header");
                        ctx.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                }
                else await next();
            });
            app.UseFileServer();
        }
    }
}
