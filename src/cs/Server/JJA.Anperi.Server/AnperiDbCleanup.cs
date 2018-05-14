using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JJA.Anperi.Server.Model;
using JJA.Anperi.Server.Utility;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace JJA.Anperi.Server
{
    public class AnperiDbCleanup : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AnperiDbCleanup> _logger;

        public AnperiDbCleanup(IServiceScopeFactory serviceScopeFactory, ILogger<AnperiDbCleanup> logger)
        {
            _logger = logger;
            _scopeFactory = serviceScopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (IServiceScope scope = _scopeFactory.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AnperiDbContext>();
                while (!stoppingToken.IsCancellationRequested)
                {

                    _logger.LogInformation("Starting db cleanup.");
                    db.ActivePairingCodes.RemoveRange(db.ActivePairingCodes.Where(apc =>
                        (DateTime.Now - apc.Created) > TimeSpan.FromMinutes(30)));
                    int res = await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Removed {0} entries.", res);
                    await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                }
            }
        }
    }
}
