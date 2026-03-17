using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class TokenPackService
{
    private readonly AppDbContext _db;

    public TokenPackService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<List<TokenPackDto>> GetByUserIdAsync(int userId)
    {
        return await _db.TokenPacks
            .Include(tp => tp.User)
            .Include(tp => tp.CreatedByUser)
            .Where(tp => tp.UserId == userId)
            .OrderByDescending(tp => tp.CreatedAt)
            .Select(tp => new TokenPackDto(
                tp.Id,
                tp.UserId,
                tp.User != null ? tp.User.Username : "",
                tp.TotalTokens,
                tp.RemainingTokens,
                tp.CreatedByUser != null ? tp.CreatedByUser.Username : "",
                tp.Description,
                tp.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<List<TokenPackDto>> GetAllAsync()
    {
        return await _db.TokenPacks
            .Include(tp => tp.User)
            .Include(tp => tp.CreatedByUser)
            .OrderByDescending(tp => tp.CreatedAt)
            .Select(tp => new TokenPackDto(
                tp.Id,
                tp.UserId,
                tp.User != null ? tp.User.Username : "",
                tp.TotalTokens,
                tp.RemainingTokens,
                tp.CreatedByUser != null ? tp.CreatedByUser.Username : "",
                tp.Description,
                tp.CreatedAt
            ))
            .ToListAsync();
    }

    public async Task<TokenPackDto> CreateAsync(int userId, int totalTokens, string? description, int createdBy)
    {
        var pack = new TokenPack
        {
            UserId = userId,
            TotalTokens = totalTokens,
            RemainingTokens = totalTokens,
            CreatedBy = createdBy,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };

        _db.TokenPacks.Add(pack);
        await _db.SaveChangesAsync();

        var user = await _db.Users.FindAsync(userId);
        var creator = await _db.Users.FindAsync(createdBy);

        return new TokenPackDto(
            pack.Id,
            pack.UserId,
            user?.Username ?? "",
            pack.TotalTokens,
            pack.RemainingTokens,
            creator?.Username ?? "",
            pack.Description,
            pack.CreatedAt
        );
    }

    public async Task<TokenPackDto?> UpdateAsync(int id, int? remainingTokens, string? description)
    {
        var pack = await _db.TokenPacks.Include(p => p.User).Include(p => p.CreatedByUser).FirstOrDefaultAsync(p => p.Id == id);
        if (pack is null) return null;
        if (remainingTokens.HasValue) pack.RemainingTokens = remainingTokens.Value;
        if (description is not null) pack.Description = description;
        await _db.SaveChangesAsync();
        return new TokenPackDto(pack.Id, pack.UserId, pack.User?.Username ?? "", pack.TotalTokens, pack.RemainingTokens, pack.CreatedByUser?.Username ?? "", pack.Description, pack.CreatedAt);
    }

    public async Task<int> GetAvailableTokensAsync(int userId)
    {
        return await _db.TokenPacks
            .Where(tp => tp.UserId == userId && tp.RemainingTokens > 0)
            .SumAsync(tp => tp.RemainingTokens);
    }

    public async Task<int?> ConsumeTokenAsync(int userId)
    {
        var pack = await _db.TokenPacks
            .Where(tp => tp.UserId == userId && tp.RemainingTokens > 0)
            .OrderBy(tp => tp.CreatedAt)
            .FirstOrDefaultAsync();

        if (pack is null) return null;

        pack.RemainingTokens--;
        await _db.SaveChangesAsync();
        return pack.Id;
    }

    public async Task<bool> RefundTokenAsync(int tokenPackId)
    {
        var pack = await _db.TokenPacks.FindAsync(tokenPackId);
        if (pack is null) return false;

        pack.RemainingTokens++;
        await _db.SaveChangesAsync();
        return true;
    }
}
