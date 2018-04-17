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
                var context = services.GetRequiredService<AnperiDbContext>();
                var logger = services.GetRequiredService<ILogger<Program>>();
                try
                {
                    context.Database.EnsureCreated();
                }
                catch (Exception ex)
                {
                    logger.LogCritical(ex, "Error reading or creating database! Shutting down ...\nYou might want to enable InMemoryDatabase in appsettings.json.");
                    return;
                }
                try
                {
                    logger.LogInformation("Testing database structure ...");
                    Host h = new Host {Name = "SomeHost", Token = "DEBUGTOKENHOST"};
                    Peripheral p = new Peripheral {Name = "SomePeripheral", Token = "SOMEPERIPHERALDEBUGTOKEN"};
                    context.Hosts.Add(h);
                    context.Peripherals.Add(p);
                    context.SaveChanges();
                    HostPeripheral hp = new HostPeripheral {Host = h, Peripheral = p};
                    h.PairedPeripherals.Add(hp);
                    context.SaveChanges();
                    var code = new ActivePairingCode {Code = "123456", PeripheralId = p.Id};
                    context.ActivePairingCodes.Add(code);
                    context.SaveChanges();
                    context.RemoveRange(code, hp, p, h);
                    context.SaveChanges();
                    logger.LogInformation("Testing database structure: SUCCESS");
                }
                catch (Exception ex)
                {
                    logger.LogCritical("Testing database structure: ERROR");
                    logger.LogCritical(ex, "Error testing DB structure! Shutting down ...\nYou might need to wipe the database.");
                    return;
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
