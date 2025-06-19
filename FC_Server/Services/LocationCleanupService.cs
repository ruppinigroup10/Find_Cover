using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using FC_Server;

namespace FC_Server.Services
{
    public class LocationCleanupService : BackgroundService
    {
        private readonly ILogger<LocationCleanupService> _logger;
        private readonly IServiceProvider _serviceProvider;

        public LocationCleanupService(ILogger<LocationCleanupService> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("LocationCleanupService started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var locationService = scope.ServiceProvider.GetRequiredService<LocationDbService>();
                        locationService.CleanupUserLocations();
                        _logger.LogInformation("CleanupUserLocations executed at: {time}", DateTimeOffset.Now);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during CleanupUserLocations");
                }

                await Task.Delay(TimeSpan.FromMinutes(720), stoppingToken); //every 12 hours
            }
        }
    }
}