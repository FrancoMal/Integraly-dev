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

    public async Task<BackupDto?> UploadBackupAsync(Stream fileStream, string originalFileName)
    {
        // Sanitize filename - keep only alphanumeric, dots, underscores, hyphens
        var safeName = Path.GetFileNameWithoutExtension(originalFileName);
        safeName = System.Text.RegularExpressions.Regex.Replace(safeName, @"[^a-zA-Z0-9_\-]", "_");
        var fileName = $"{safeName}_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bak";
        var localPath = Path.Combine(_backupPathLocal, fileName);

        try
        {
            using (var fs = new FileStream(localPath, FileMode.Create))
            {
                await fileStream.CopyToAsync(fs);
            }

            // Make file readable by SQL Server (mssql user)
            // File permissions are handled by the shared volume

            var sizeBytes = new FileInfo(localPath).Length;

            var backup = new DatabaseBackup
            {
                FileName = fileName,
                DatabaseName = "AIcoding",
                Type = "uploaded",
                Status = "completed",
                SizeBytes = sizeBytes,
                CreatedAt = DateTime.UtcNow
            };

            _db.DatabaseBackups.Add(backup);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Backup uploaded: {FileName} ({Size} bytes)", fileName, sizeBytes);
            return ToDto(backup);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload backup: {FileName}", originalFileName);
            // Cleanup file if it was partially written
            if (File.Exists(localPath))
            {
                try { File.Delete(localPath); } catch { }
            }
            return null;
        }
    }

    public async Task<BackupInfoDto?> GetBackupInfoAsync(int id)
    {
        var backup = await _db.DatabaseBackups.FindAsync(id);
        if (backup == null) return null;

        var localPath = Path.Combine(_backupPathLocal, backup.FileName);
        if (!File.Exists(localPath)) return null;

        var sqlPath = Path.Combine(_backupPathSql, backup.FileName);

        try
        {
            var connStr = _db.Database.GetConnectionString()!;
            var builder = new SqlConnectionStringBuilder(connStr);
            builder.InitialCatalog = "master";

            using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "RESTORE HEADERONLY FROM DISK = @path";
            cmd.Parameters.AddWithValue("@path", sqlPath);
            cmd.CommandTimeout = 60;

            using var reader = await cmd.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                var backupSize = reader["BackupSize"] is DBNull ? 0L : Convert.ToInt64(reader["BackupSize"]);
                return new BackupInfoDto
                {
                    Id = backup.Id,
                    FileName = backup.FileName,
                    DatabaseName = reader["DatabaseName"]?.ToString() ?? "",
                    ServerName = reader["ServerName"]?.ToString() ?? "",
                    MachineName = reader["MachineName"]?.ToString() ?? "",
                    BackupStartDate = reader["BackupStartDate"] is DBNull ? null : (DateTime?)reader["BackupStartDate"],
                    BackupFinishDate = reader["BackupFinishDate"] is DBNull ? null : (DateTime?)reader["BackupFinishDate"],
                    BackupSize = backupSize,
                    BackupSizeFormatted = FormatSize(backupSize),
                    BackupType = reader["BackupTypeDescription"]?.ToString() ?? ""
                };
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to read backup info: {FileName}", backup.FileName);
            return null;
        }
    }

    public async Task<(bool Success, string Message)> RestoreBackupAsync(int id)
    {
        var backup = await _db.DatabaseBackups.FindAsync(id);
        if (backup == null) return (false, "Backup no encontrado");

        var localPath = Path.Combine(_backupPathLocal, backup.FileName);
        if (!File.Exists(localPath)) return (false, "Archivo de backup no encontrado");

        var sqlPath = Path.Combine(_backupPathSql, backup.FileName);

        try
        {
            var connStr = _db.Database.GetConnectionString()!;
            var builder = new SqlConnectionStringBuilder(connStr);
            builder.InitialCatalog = "master";

            using var conn = new SqlConnection(builder.ConnectionString);
            await conn.OpenAsync();

            // Set single user to kill all connections
            _logger.LogWarning("Starting database restore from {FileName}. Setting SINGLE_USER mode...", backup.FileName);
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = "ALTER DATABASE [AIcoding] SET SINGLE_USER WITH ROLLBACK IMMEDIATE";
                cmd.CommandTimeout = 120;
                await cmd.ExecuteNonQueryAsync();
            }

            try
            {
                // Restore
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = "RESTORE DATABASE [AIcoding] FROM DISK = @path WITH REPLACE";
                    cmd.Parameters.AddWithValue("@path", sqlPath);
                    cmd.CommandTimeout = 600; // 10 minutes for large DBs
                    await cmd.ExecuteNonQueryAsync();
                }

                _logger.LogWarning("Database restored successfully from {FileName}", backup.FileName);
            }
            finally
            {
                // Always set back to multi user
                try
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "ALTER DATABASE [AIcoding] SET MULTI_USER";
                    cmd.CommandTimeout = 30;
                    await cmd.ExecuteNonQueryAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to set MULTI_USER mode after restore");
                }
            }

            return (true, "Base de datos restaurada correctamente");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database restore failed from {FileName}", backup.FileName);

            // Try to set back to multi user in case of failure
            try
            {
                var connStr2 = _db.Database.GetConnectionString()!;
                var builder2 = new SqlConnectionStringBuilder(connStr2);
                builder2.InitialCatalog = "master";
                using var conn2 = new SqlConnection(builder2.ConnectionString);
                await conn2.OpenAsync();
                using var cmd2 = conn2.CreateCommand();
                cmd2.CommandText = "ALTER DATABASE [AIcoding] SET MULTI_USER";
                await cmd2.ExecuteNonQueryAsync();
            }
            catch { }

            return (false, $"Error al restaurar: {ex.Message}");
        }
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
