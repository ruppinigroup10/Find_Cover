using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

public class AlertBackgroundService : BackgroundService
{
    private readonly ILogger<AlertBackgroundService> _logger;

    public AlertBackgroundService(ILogger<AlertBackgroundService> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                DBservicesAlert db = new DBservicesAlert();
                var alerts = await db.GetAlertsFromApi();
                db.SaveAlertsToDb(alerts);

                _logger.LogInformation($"[AlertBackgroundService] Processed {alerts.Count} alerts at {DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"[AlertBackgroundService] Error: {ex.Message}");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken); // runs this every 10 secs
        }
    }
}
