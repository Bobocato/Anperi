using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace JJA.Anperi.Server
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
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                //make debug website available in webroot
                app.UseFileServer();
            }
            //uncomment if needed
            //app.UseMvc();

            bool webSocketReceiveBufferSizeValid = int.TryParse(Configuration["ServerStartupSettings:WebSocketReceiveBufferSize"], out int webSocketReceiveBufferSize);
            if (!webSocketReceiveBufferSizeValid) webSocketReceiveBufferSize = (16 * 1024);

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ReceiveBufferSize = webSocketReceiveBufferSize
            });
            app.UseAnperiWebSocket("/ws-api", webSocketReceiveBufferSize, appLifetime.ApplicationStopping);

            appLifetime.ApplicationStopping.Register(
                () =>
                {
                    Console.Write("test");
                }
            );
        }
    }
}
