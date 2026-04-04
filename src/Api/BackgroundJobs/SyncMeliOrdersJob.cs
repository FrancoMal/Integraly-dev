using Api.Services;

namespace Api.BackgroundJobs;

public class SyncMeliOrdersJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncMeliOrdersJob> _logger;

    public SyncMeliOrdersJob(IServiceProvider serviceProvider, ILogger<SyncMeliOrdersJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var integrationService = scope.ServiceProvider.GetRequiredService<IntegrationService>();
                var integration = await integrationService.GetByProviderAsync("mercadolibre");

                if (integration is not null && integration.IsActive)
                {
                    var orderService = scope.ServiceProvider.GetRequiredService<MeliOrderService>();
                    var count = await orderService.SyncOrdersAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
                    _logger.LogInformation("SyncMeliOrdersJob: synced {Count} orders", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncMeliOrdersJob");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
