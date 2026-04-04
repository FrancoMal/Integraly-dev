using Api.Services;

namespace Api.BackgroundJobs;

public class SyncMeliItemsJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncMeliItemsJob> _logger;

    public SyncMeliItemsJob(IServiceProvider serviceProvider, ILogger<SyncMeliItemsJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(3), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var integrationService = scope.ServiceProvider.GetRequiredService<IntegrationService>();
                var integration = await integrationService.GetByProviderAsync("mercadolibre");

                if (integration is not null && integration.IsActive)
                {
                    var itemService = scope.ServiceProvider.GetRequiredService<MeliItemService>();
                    var count = await itemService.SyncItemsAsync();
                    _logger.LogInformation("SyncMeliItemsJob: synced {Count} items", count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SyncMeliItemsJob");
            }

            await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
        }
    }
}
