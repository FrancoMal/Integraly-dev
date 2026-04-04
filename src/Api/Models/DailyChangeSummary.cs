using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("DailyChangeSummaries")]
public class DailyChangeSummary
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string GeneralSummary { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalGroups { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<CommitGroup> CommitGroups { get; set; } = new();
}
