using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BookingCompletionService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BookingCompletionService> _logger;

    public BookingCompletionService(IServiceProvider serviceProvider, ILogger<BookingCompletionService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;
                // Mark as completed: confirmed bookings where date+hour+1 has passed
                var bookingsToComplete = await db.Bookings
                    .Where(b => b.Status == "confirmed"
                        && (b.ScheduledDate.Date < now.Date
                            || (b.ScheduledDate.Date == now.Date && b.StartHour + 1 <= now.Hour)))
                    .ToListAsync(stoppingToken);

                if (bookingsToComplete.Count > 0)
                {
                    foreach (var b in bookingsToComplete)
                    {
                        b.Status = "completed";
                    }
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("Marked {Count} bookings as completed", bookingsToComplete.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BookingCompletionService");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}
