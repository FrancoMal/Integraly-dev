using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BackupSchedulerService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BackupSchedulerService> _logger;

    public BackupSchedulerService(IServiceProvider serviceProvider, ILogger<BackupSchedulerService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Wait 1 minute after startup before first check
        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var backupService = scope.ServiceProvider.GetRequiredService<BackupService>();

                var schedule = await backupService.GetScheduleAsync();

                if (schedule.Enabled)
                {
                    // Check if enough time has passed since last backup
                    var lastBackup = await db.DatabaseBackups
                        .Where(b => b.Type == "scheduled" && b.Status == "completed")
                        .OrderByDescending(b => b.CreatedAt)
                        .FirstOrDefaultAsync(stoppingToken);

                    var shouldBackup = lastBackup == null ||
                        DateTime.UtcNow - lastBackup.CreatedAt >= TimeSpan.FromHours(schedule.IntervalHours);

                    if (shouldBackup)
                    {
                        _logger.LogInformation("Starting scheduled backup...");
                        await backupService.CreateBackupAsync("scheduled");

                        // Cleanup old backups
                        if (schedule.RetentionDays > 0)
                        {
                            await backupService.CleanupOldBackupsAsync(schedule.RetentionDays);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in BackupSchedulerService");
            }

            // Check every 10 minutes
            await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
        }
    }
}
