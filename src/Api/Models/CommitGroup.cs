using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("CommitGroups")]
public class CommitGroup
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }
    public int DailySummaryId { get; set; }
    public string GroupTitle { get; set; } = string.Empty;
    public string GroupSummary { get; set; } = string.Empty;
    public string Tags { get; set; } = string.Empty;
    public string CommitsJson { get; set; } = "[]";
    public int DisplayOrder { get; set; }
    public string? Author { get; set; }

    [ForeignKey("DailySummaryId")]
    public DailyChangeSummary? DailySummary { get; set; }
}
