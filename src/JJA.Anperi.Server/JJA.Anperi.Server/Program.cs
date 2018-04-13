using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JJA.Anperi.Server.Model;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace JJA.Anperi.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            IWebHost webHost = BuildWebHost(args, config);

            using (var scope = webHost.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<AnperiDbContext>();
                    context.Database.EnsureCreated();
                    context.Hosts.Add(new Host { Token = Guid.NewGuid().ToString() });
                    context.Peripherals.Add(new Peripheral { Token = Guid.NewGuid().ToString() });
                    context.Hosts.Add(new Host { Token = Guid.NewGuid().ToString() });
                    context.Peripherals.Add(new Peripheral { Token = Guid.NewGuid().ToString() });
                    context.SaveChanges();
                    
                    context.Hosts.Find(1).PairedPeripherals.Add(new HostPeripheral{HostId = 1, PeripheralId = 4});
                    context.Hosts.Find(3).PairedPeripherals.Add(new HostPeripheral { HostId = 3, PeripheralId = 4 });
                    context.SaveChanges();
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "An error occurred while creating the database.");
                }
            }

            webHost.Run();
        }

        public static IWebHost BuildWebHost(string[] args, IConfiguration config)
        {
            IWebHostBuilder builder = WebHost.CreateDefaultBuilder(args);
            string address = config["ServerSettings:Url"];
            if (!string.IsNullOrEmpty(address))
            {
                builder.UseUrls(address);
            }
            return builder.UseStartup<Startup>()
                .Build();
        }
    }
}
