using System.Text.Json;
using Api.DTOs;

namespace Api.Services;

public class WhatsAppService
{
    private readonly HttpClient _http;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(IHttpClientFactory httpClientFactory, ILogger<WhatsAppService> logger)
    {
        _http = httpClientFactory.CreateClient();
        var baseUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_URL") ?? "http://playwright:3001";
        _http.BaseAddress = new Uri(baseUrl);
        _http.Timeout = TimeSpan.FromMinutes(10);
        _logger = logger;
    }

    public async Task<WhatsAppStatusDto> GetStatusAsync()
    {
        try
        {
            var response = await _http.GetFromJsonAsync<JsonElement>("/whatsapp/status");
            return new WhatsAppStatusDto(
                response.TryGetProperty("linked", out var l) && l.GetBoolean(),
                response.TryGetProperty("info", out var i) ? i.GetString() : null,
                response.TryGetProperty("isLinking", out var il) && il.GetBoolean()
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting WhatsApp status");
            return new WhatsAppStatusDto(false, "Servicio no disponible", false);
        }
    }

    public async Task StartLinkingAsync()
    {
        await _http.PostAsync("/whatsapp/link", null);
    }

    public async Task<byte[]> GetQrScreenshotAsync()
    {
        var response = await _http.GetAsync("/whatsapp/qr");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsByteArrayAsync();
    }

    public async Task<bool> CheckLinkedAsync()
    {
        var response = await _http.GetFromJsonAsync<JsonElement>("/whatsapp/check-linked");
        return response.TryGetProperty("linked", out var l) && l.GetBoolean();
    }

    public async Task UnlinkAsync()
    {
        await _http.PostAsync("/whatsapp/unlink", null);
    }

    public async Task CancelLinkingAsync()
    {
        await _http.PostAsync("/whatsapp/cancel-link", null);
    }

    public async Task<object> SendMessageAsync(string phone, string message)
    {
        var response = await _http.PostAsJsonAsync("/whatsapp/send-bulk", new
        {
            recipients = new[] { new { phone, name = "" } },
            message
        });
        var json = await response.Content.ReadAsStringAsync();
        return json;
    }

    public async Task<List<BulkSendResult>> SendBulkAsync(List<BulkRecipient> recipients, string message)
    {
        var response = await _http.PostAsJsonAsync("/whatsapp/send-bulk", new { recipients, message });
        if (!response.IsSuccessStatusCode) return new();
        return await response.Content.ReadFromJsonAsync<List<BulkSendResult>>() ?? new();
    }
}
