using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JJA.Anperi.Server.Model;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
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
            //services.AddMvc();
            if (Convert.ToBoolean(Configuration["ServerStartupSettings:UseInMemoryDatabase"]))
            {
                services.AddDbContext<AnperiDbContext>(o => o.UseInMemoryDatabase("JJA.Anperi.Server.InMemoryDb"));
            }
            else services.AddDbContext<AnperiDbContext>(o => o.UseMySQL(Configuration.GetConnectionString("MySqlConnectionString")));

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, IApplicationLifetime appLifetime, IServiceProvider serviceProvider)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            //uncomment if needed
            //app.UseMvc();
            if (Convert.ToBoolean(Configuration["ServerStartupSettings:StartFileServer"]))
            {
                //make debug website available in webroot
                app.UseFileServer();
            }

            bool webSocketReceiveBufferSizeValid = int.TryParse(Configuration["ServerStartupSettings:WebSocketReceiveBufferSize"], out int webSocketReceiveBufferSize);
            if (!webSocketReceiveBufferSizeValid) webSocketReceiveBufferSize = (16 * 1024);

            app.UseWebSockets(new WebSocketOptions
            {
                KeepAliveInterval = TimeSpan.FromSeconds(20),
                ReceiveBufferSize = webSocketReceiveBufferSize
            });
            string anperiWebSocketApiPath = Configuration["ServerStartupSettings:AnperiWebSocketApiPath"];
            if (string.IsNullOrEmpty(anperiWebSocketApiPath)) anperiWebSocketApiPath = "/api/ws";
            app.UseAnperiWebSocket(anperiWebSocketApiPath, webSocketReceiveBufferSize, serviceProvider, appLifetime.ApplicationStopping);
        }
    }
}
