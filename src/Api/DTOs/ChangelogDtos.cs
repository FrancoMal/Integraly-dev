namespace Api.DTOs;

public record DailyChangeSummaryListDto(
    int Id, DateTime Date, string GeneralSummary,
    int TotalCommits, int TotalGroups, DateTime CreatedAt
);

public record CommitGroupDto(
    int Id, string GroupTitle, string GroupSummary,
    List<string> Tags, string CommitsJson, int DisplayOrder, string? Author
);

public record DailyChangeSummaryDetailDto(
    int Id, DateTime Date, string GeneralSummary,
    int TotalCommits, int TotalGroups, DateTime CreatedAt,
    List<CommitGroupDto> Groups
);

public record ChangelogListResponse(
    List<DailyChangeSummaryListDto> Items, int Total, int Page, int PageSize
);

public record CreateDailyChangelogRequest(
    DateTime Date, string GeneralSummary, int TotalCommits,
    List<CreateCommitGroupRequest> Groups
);

public record CreateCommitGroupRequest(
    string GroupTitle, string GroupSummary,
    List<string> Tags, string CommitsJson, int DisplayOrder
);

public record UpdateAuthorRequest(string? Author);
