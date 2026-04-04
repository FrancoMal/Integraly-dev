using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class IntegrationService
{
    private readonly AppDbContext _db;
    private readonly ILogger<IntegrationService> _logger;

    public IntegrationService(AppDbContext db, ILogger<IntegrationService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<List<IntegrationDto>> GetAllAsync()
    {
        var items = await _db.Integrations.ToListAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<IntegrationDto?> GetByProviderAsync(string provider)
    {
        var item = await _db.Integrations.FirstOrDefaultAsync(i => i.Provider == provider);
        return item is null ? null : ToDto(item);
    }

    public async Task<string?> GetSecretAsync(string provider)
    {
        var item = await _db.Integrations.FirstOrDefaultAsync(i => i.Provider == provider);
        return item?.AppSecret;
    }

    public async Task<Integration?> GetRawByProviderAsync(string provider)
    {
        return await _db.Integrations.FirstOrDefaultAsync(i => i.Provider == provider);
    }

    public async Task<IntegrationDto> SaveAsync(SaveIntegrationRequest req)
    {
        var existing = await _db.Integrations.FirstOrDefaultAsync(i => i.Provider == req.Provider);

        if (existing is null)
        {
            existing = new Integration
            {
                Provider = req.Provider,
                AppId = req.AppId,
                AppSecret = req.AppSecret,
                RedirectUrl = req.RedirectUrl,
                Settings = req.Settings,
                IsActive = req.IsActive,
                CreatedAt = DateTime.UtcNow
            };
            _db.Integrations.Add(existing);
        }
        else
        {
            existing.AppId = req.AppId;
            // Only overwrite secret if a new value is provided
            if (!string.IsNullOrEmpty(req.AppSecret))
                existing.AppSecret = req.AppSecret;
            existing.RedirectUrl = req.RedirectUrl;
            existing.Settings = req.Settings;
            existing.IsActive = req.IsActive;
            existing.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Integration saved: {Provider}", req.Provider);
        return ToDto(existing);
    }

    public async Task<bool> DeleteAsync(string provider)
    {
        var item = await _db.Integrations.FirstOrDefaultAsync(i => i.Provider == provider);
        if (item is null) return false;

        _db.Integrations.Remove(item);
        await _db.SaveChangesAsync();
        _logger.LogInformation("Integration deleted: {Provider}", provider);
        return true;
    }

    private static IntegrationDto ToDto(Integration i) => new(
        i.Id,
        i.Provider,
        i.AppId,
        !string.IsNullOrEmpty(i.AppSecret),
        i.RedirectUrl,
        i.Settings,
        i.IsActive,
        i.CreatedAt,
        i.UpdatedAt
    );
}
