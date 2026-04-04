using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MeliAccountService
{
    private readonly AppDbContext _db;
    private readonly IntegrationService _integrationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MeliAccountService> _logger;

    public MeliAccountService(AppDbContext db, IntegrationService integrationService,
        IHttpClientFactory httpClientFactory, ILogger<MeliAccountService> logger)
    {
        _db = db;
        _integrationService = integrationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<MeliAccountDto>> GetAccountsAsync()
    {
        var accounts = await _db.MeliAccounts.ToListAsync();
        return accounts.Select(a => new MeliAccountDto(
            a.Id, a.MeliUserId, a.Nickname, a.Email,
            a.TokenExpiresAt > DateTime.UtcNow,
            a.TokenExpiresAt, a.CreatedAt
        )).ToList();
    }

    public async Task<string?> GetAuthUrlAsync()
    {
        var integration = await _integrationService.GetRawByProviderAsync("mercadolibre");
        if (integration is null || string.IsNullOrEmpty(integration.AppId))
            return null;

        var redirectUrl = Uri.EscapeDataString(integration.RedirectUrl ?? "");
        return $"https://auth.mercadolibre.com.ar/authorization?response_type=code&client_id={integration.AppId}&redirect_uri={redirectUrl}";
    }

    public async Task<(MeliAccountDto? Account, string? Error)> HandleCallbackAsync(string code)
    {
        var integration = await _integrationService.GetRawByProviderAsync("mercadolibre");
        if (integration is null)
            return (null, "MercadoLibre no esta configurado");

        var client = _httpClientFactory.CreateClient();

        // Exchange code for token
        var tokenResponse = await client.PostAsJsonAsync("https://api.mercadolibre.com/oauth/token", new
        {
            grant_type = "authorization_code",
            client_id = integration.AppId,
            client_secret = integration.AppSecret,
            code = code,
            redirect_uri = integration.RedirectUrl
        });

        if (!tokenResponse.IsSuccessStatusCode)
        {
            var errorBody = await tokenResponse.Content.ReadAsStringAsync();
            _logger.LogError("MeLi token exchange failed: {Error}", errorBody);
            return (null, "Error al obtener token de MercadoLibre");
        }

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>();
        var accessToken = tokenJson.GetProperty("access_token").GetString()!;
        var refreshToken = tokenJson.TryGetProperty("refresh_token", out var rt) ? rt.GetString() : null;
        var expiresIn = tokenJson.GetProperty("expires_in").GetInt32();

        // Get user info
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
        var userResponse = await client.GetFromJsonAsync<JsonElement>("https://api.mercadolibre.com/users/me");
        var meliUserId = userResponse.GetProperty("id").GetInt64();
        var nickname = userResponse.GetProperty("nickname").GetString() ?? "";
        var email = userResponse.TryGetProperty("email", out var em) ? em.GetString() : null;

        // Upsert account
        var account = await _db.MeliAccounts.FirstOrDefaultAsync(a => a.MeliUserId == meliUserId);
        if (account is null)
        {
            account = new MeliAccount
            {
                MeliUserId = meliUserId,
                Nickname = nickname,
                Email = email,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                CreatedAt = DateTime.UtcNow
            };
            _db.MeliAccounts.Add(account);
        }
        else
        {
            account.Nickname = nickname;
            account.Email = email;
            account.AccessToken = accessToken;
            account.RefreshToken = refreshToken;
            account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            account.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("MeLi account connected: {Nickname} ({UserId})", nickname, meliUserId);

        return (new MeliAccountDto(account.Id, account.MeliUserId, account.Nickname, account.Email,
            true, account.TokenExpiresAt, account.CreatedAt), null);
    }

    public async Task<string?> GetValidTokenAsync(MeliAccount account)
    {
        if (account.TokenExpiresAt > DateTime.UtcNow.AddMinutes(5))
            return account.AccessToken;

        return await RefreshTokenAsync(account);
    }

    public async Task<string?> RefreshTokenAsync(MeliAccount account)
    {
        var integration = await _integrationService.GetRawByProviderAsync("mercadolibre");
        if (integration is null || string.IsNullOrEmpty(account.RefreshToken))
            return null;

        var client = _httpClientFactory.CreateClient();
        var response = await client.PostAsJsonAsync("https://api.mercadolibre.com/oauth/token", new
        {
            grant_type = "refresh_token",
            client_id = integration.AppId,
            client_secret = integration.AppSecret,
            refresh_token = account.RefreshToken
        });

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MeLi token refresh failed for account {Id}", account.Id);
            return null;
        }

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        account.AccessToken = json.GetProperty("access_token").GetString()!;
        if (json.TryGetProperty("refresh_token", out var rt))
            account.RefreshToken = rt.GetString();
        account.TokenExpiresAt = DateTime.UtcNow.AddSeconds(json.GetProperty("expires_in").GetInt32());
        account.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();
        _logger.LogInformation("MeLi token refreshed for account {Id}", account.Id);
        return account.AccessToken;
    }

    public async Task<bool> DeleteAccountAsync(int id)
    {
        var account = await _db.MeliAccounts.FindAsync(id);
        if (account is null) return false;

        _db.MeliAccounts.Remove(account);
        await _db.SaveChangesAsync();
        _logger.LogInformation("MeLi account deleted: {Id}", id);
        return true;
    }
}
