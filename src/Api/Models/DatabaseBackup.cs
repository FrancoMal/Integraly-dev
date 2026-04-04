namespace Api.Models;

public class DatabaseBackup
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = "AIcoding";
    public long SizeBytes { get; set; }
    public string Type { get; set; } = "manual"; // manual, scheduled
    public string Status { get; set; } = "completed"; // in_progress, completed, failed
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
