using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class ChangelogService
{
    private readonly AppDbContext _db;

    public ChangelogService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ChangelogListResponse> GetListAsync(DateTime? from, DateTime? to, string? search, List<string>? tags, int page, int pageSize)
    {
        var query = _db.DailyChangeSummaries.AsQueryable();

        if (from.HasValue)
            query = query.Where(d => d.Date >= DateOnly.FromDateTime(from.Value).ToDateTime(TimeOnly.MinValue));
        if (to.HasValue)
            query = query.Where(d => d.Date <= DateOnly.FromDateTime(to.Value).ToDateTime(TimeOnly.MaxValue));

        if (tags is not null && tags.Count > 0)
        {
            // Buscar grupos que contengan TODAS las tags seleccionadas
            var allGroups = await _db.CommitGroups.Select(g => new { g.DailySummaryId, g.Tags }).ToListAsync();
            var matchingIds = allGroups
                .Where(g => tags.All(t => g.Tags.ToLower().Contains(t)))
                .Select(g => g.DailySummaryId)
                .Distinct()
                .ToList();

            query = query.Where(d => matchingIds.Contains(d.Id));
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.ToLower();
            var summaryIds = await _db.CommitGroups
                .Where(g => g.GroupTitle.ToLower().Contains(searchLower) || g.GroupSummary.ToLower().Contains(searchLower))
                .Select(g => g.DailySummaryId)
                .Distinct()
                .ToListAsync();

            query = query.Where(d => d.GeneralSummary.ToLower().Contains(searchLower) || summaryIds.Contains(d.Id));
        }

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(d => d.Date)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(d => new DailyChangeSummaryListDto(d.Id, d.Date, d.GeneralSummary, d.TotalCommits, d.TotalGroups, d.CreatedAt))
            .ToListAsync();

        return new ChangelogListResponse(items, total, page, pageSize);
    }

    public async Task<DailyChangeSummaryDetailDto?> GetByDateAsync(DateTime date)
    {
        var summary = await _db.DailyChangeSummaries
            .Include(d => d.CommitGroups)
            .FirstOrDefaultAsync(d => d.Date.Date == date.Date);

        if (summary is null) return null;

        var groups = summary.CommitGroups
            .OrderBy(g => g.DisplayOrder)
            .Select(g => new CommitGroupDto(
                g.Id, g.GroupTitle, g.GroupSummary,
                g.Tags.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(t => t.Trim()).ToList(),
                g.CommitsJson, g.DisplayOrder, g.Author
            ))
            .ToList();

        return new DailyChangeSummaryDetailDto(
            summary.Id, summary.Date, summary.GeneralSummary,
            summary.TotalCommits, summary.TotalGroups, summary.CreatedAt, groups
        );
    }

    public async Task<DailyChangeSummaryDetailDto> CreateOrUpdateAsync(CreateDailyChangelogRequest request)
    {
        // Delete existing if any (CASCADE will delete groups)
        var existing = await _db.DailyChangeSummaries.FirstOrDefaultAsync(d => d.Date.Date == request.Date.Date);
        if (existing is not null)
        {
            _db.DailyChangeSummaries.Remove(existing);
            await _db.SaveChangesAsync();
        }

        var summary = new DailyChangeSummary
        {
            Date = request.Date.Date,
            GeneralSummary = request.GeneralSummary,
            TotalCommits = request.TotalCommits,
            TotalGroups = request.Groups.Count,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var g in request.Groups)
        {
            summary.CommitGroups.Add(new CommitGroup
            {
                GroupTitle = g.GroupTitle,
                GroupSummary = g.GroupSummary,
                Tags = string.Join(",", g.Tags),
                CommitsJson = g.CommitsJson,
                DisplayOrder = g.DisplayOrder
            });
        }

        _db.DailyChangeSummaries.Add(summary);
        await _db.SaveChangesAsync();

        return (await GetByDateAsync(summary.Date))!;
    }

    public async Task<bool> UpdateAuthorAsync(int groupId, string? author)
    {
        var group = await _db.CommitGroups.FindAsync(groupId);
        if (group is null) return false;

        group.Author = author;
        await _db.SaveChangesAsync();
        return true;
    }
}
