namespace Web.Models;

public class BackupDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public string SizeFormatted { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class BackupScheduleDto
{
    public bool Enabled { get; set; }
    public int IntervalHours { get; set; } = 24;
    public int RetentionDays { get; set; } = 30;
}

public class BackupInfoDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string ServerName { get; set; } = string.Empty;
    public string MachineName { get; set; } = string.Empty;
    public DateTime? BackupStartDate { get; set; }
    public DateTime? BackupFinishDate { get; set; }
    public long BackupSize { get; set; }
    public string BackupSizeFormatted { get; set; } = string.Empty;
    public string BackupType { get; set; } = string.Empty;
}
