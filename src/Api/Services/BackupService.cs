using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class BackupService
{
    private readonly AppDbContext _db;
    private readonly ILogger<BackupService> _logger;
    private readonly string _backupPathSql = "/backups";
    private readonly string _backupPathLocal = "/app/backups";

    public BackupService(AppDbContext db, ILogger<BackupService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<BackupDto?> CreateBackupAsync(string type = "manual")
    {
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
        var fileName = $"AIcoding_{timestamp}.bak";
        var sqlPath = $"{_backupPathSql}/{fileName}";

        var backup = new DatabaseBackup
        {
            FileName = fileName,
            DatabaseName = "AIcoding",
            Type = type,
            Status = "in_progress",
            CreatedAt = DateTime.UtcNow
        };

        _db.DatabaseBackups.Add(backup);
        await _db.SaveChangesAsync();

        try
        {
            // Ensure backup directory exists on SQL Server
            await _db.Database.ExecuteSqlRawAsync(
                "EXEC xp_create_subdir @path",
                new SqlParameter("@path", _backupPathSql));

            // Execute backup
            var sql = $"BACKUP DATABASE [AIcoding] TO DISK = @path WITH FORMAT, INIT, NAME = @name";
            await _db.Database.ExecuteSqlRawAsync(sql,
                new SqlParameter("@path", sqlPath),
                new SqlParameter("@name", $"AIcoding-{type}-{timestamp}"));

            // Get file size from local mount
            var localPath = Path.Combine(_backupPathLocal, fileName);
            long sizeBytes = 0;
            if (File.Exists(localPath))
            {
                sizeBytes = new FileInfo(localPath).Length;
            }

            backup.Status = "completed";
            backup.SizeBytes = sizeBytes;
            await _db.SaveChangesAsync();

            _logger.LogInformation("Backup created: {FileName} ({Size} bytes)", fileName, sizeBytes);

            return ToDto(backup);
        }
        catch (Exception ex)
        {
            backup.Status = "failed";
            backup.ErrorMessage = ex.Message;
            await _db.SaveChangesAsync();
            _logger.LogError(ex, "Backup failed: {FileName}", fileName);
            return ToDto(backup);
        }
    }

    public async Task<List<BackupDto>> GetAllAsync()
    {
        var backups = await _db.DatabaseBackups
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

        // Verify file existence and update sizes
        foreach (var b in backups)
        {
            var localPath = Path.Combine(_backupPathLocal, b.FileName);
            if (b.Status == "completed" && !File.Exists(localPath))
            {
                b.Status = "file_missing";
                b.SizeBytes = 0;
            }
            else if (File.Exists(localPath) && b.SizeBytes == 0)
            {
                b.SizeBytes = new FileInfo(localPath).Length;
            }
        }
        await _db.SaveChangesAsync();

        return backups.Select(ToDto).ToList();
    }

    public async Task<bool> DeleteBackupAsync(int id)
    {
        var backup = await _db.DatabaseBackups.FindAsync(id);
        if (backup == null) return false;

        // Delete physical file
        var localPath = Path.Combine(_backupPathLocal, backup.FileName);
        if (File.Exists(localPath))
        {
            File.Delete(localPath);
        }

        _db.DatabaseBackups.Remove(backup);
        await _db.SaveChangesAsync();
        return true;
    }

    public string? GetBackupFilePath(int id)
    {
        var backup = _db.DatabaseBackups.Find(id);
        if (backup == null) return null;

        var localPath = Path.Combine(_backupPathLocal, backup.FileName);
        return File.Exists(localPath) ? localPath : null;
    }

    public async Task CleanupOldBackupsAsync(int retentionDays)
    {
        var cutoff = DateTime.UtcNow.AddDays(-retentionDays);
        var oldBackups = await _db.DatabaseBackups
            .Where(b => b.CreatedAt < cutoff)
            .ToListAsync();

        foreach (var backup in oldBackups)
        {
            var localPath = Path.Combine(_backupPathLocal, backup.FileName);
            if (File.Exists(localPath))
            {
                try { File.Delete(localPath); }
                catch (Exception ex) { _logger.LogWarning(ex, "Could not delete backup file: {File}", backup.FileName); }
            }
        }

        _db.DatabaseBackups.RemoveRange(oldBackups);
        await _db.SaveChangesAsync();

        if (oldBackups.Count > 0)
            _logger.LogInformation("Cleaned up {Count} old backups", oldBackups.Count);
    }

    public async Task<BackupScheduleDto> GetScheduleAsync()
    {
        var settings = await _db.AppSettings
            .Where(s => s.Key.StartsWith("Backup"))
            .ToDictionaryAsync(s => s.Key, s => s.Value);

        return new BackupScheduleDto
        {
            Enabled = settings.GetValueOrDefault("BackupScheduleEnabled", "false") == "true",
            IntervalHours = int.TryParse(settings.GetValueOrDefault("BackupScheduleHours", "24"), out var h) ? h : 24,
            RetentionDays = int.TryParse(settings.GetValueOrDefault("BackupRetentionDays", "30"), out var d) ? d : 30
        };
    }

    public async Task UpdateScheduleAsync(BackupScheduleDto dto)
    {
        await UpsertSetting("BackupScheduleEnabled", dto.Enabled ? "true" : "false");
        await UpsertSetting("BackupScheduleHours", dto.IntervalHours.ToString());
        await UpsertSetting("BackupRetentionDays", dto.RetentionDays.ToString());
        await _db.SaveChangesAsync();
    }

    private async Task UpsertSetting(string key, string value)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        if (setting == null)
        {
            _db.AppSettings.Add(new AppSetting { Key = key, Value = value, UpdatedAt = DateTime.UtcNow });
        }
        else
        {
            setting.Value = value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static BackupDto ToDto(DatabaseBackup b) => new()
    {
        Id = b.Id,
        FileName = b.FileName,
        DatabaseName = b.DatabaseName,
        SizeBytes = b.SizeBytes,
        SizeFormatted = FormatSize(b.SizeBytes),
        Type = b.Type,
        Status = b.Status,
        ErrorMessage = b.ErrorMessage,
        CreatedAt = b.CreatedAt
    };

    private static string FormatSize(long bytes)
    {
        if (bytes <= 0) return "0 B";
        string[] units = { "B", "KB", "MB", "GB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < units.Length - 1) { size /= 1024; i++; }
        return $"{size:F1} {units[i]}";
    }
}
