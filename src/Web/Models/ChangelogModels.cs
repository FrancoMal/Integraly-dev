namespace Web.Models;

public class DailyChangeSummaryListDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string GeneralSummary { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalGroups { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommitGroupDto
{
    public int Id { get; set; }
    public string GroupTitle { get; set; } = string.Empty;
    public string GroupSummary { get; set; } = string.Empty;
    public List<string> Tags { get; set; } = new();
    public string CommitsJson { get; set; } = "[]";
    public int DisplayOrder { get; set; }
    public string? Author { get; set; }
}

public class CommitDetail
{
    public string Hash { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = string.Empty;
    public List<string> FilesChanged { get; set; } = new();
}

public class DailyChangeSummaryDetailDto
{
    public int Id { get; set; }
    public DateTime Date { get; set; }
    public string GeneralSummary { get; set; } = string.Empty;
    public int TotalCommits { get; set; }
    public int TotalGroups { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<CommitGroupDto> Groups { get; set; } = new();
}

public class ChangelogListResponse
{
    public List<DailyChangeSummaryListDto> Items { get; set; } = new();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
