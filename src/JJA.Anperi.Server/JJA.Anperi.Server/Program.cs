using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JJA.Anperi.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            IConfiguration config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json").Build();
            BuildWebHost(args, config).Run();
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
