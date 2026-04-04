using System.Text.Json;
using Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class AiService
{
    private readonly IntegrationService _integrationService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<AiService> _logger;

    public AiService(IntegrationService integrationService, IHttpClientFactory httpClientFactory, ILogger<AiService> logger)
    {
        _integrationService = integrationService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<object>> GetOpenAiModelsAsync()
    {
        var apiKey = await _integrationService.GetSecretAsync("openai");
        if (string.IsNullOrEmpty(apiKey)) return new();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.GetFromJsonAsync<JsonElement>("https://api.openai.com/v1/models");
        var models = new List<object>();
        var prefixes = new[] { "gpt-", "o1", "o3", "o4", "chatgpt" };

        foreach (var model in response.GetProperty("data").EnumerateArray())
        {
            var id = model.GetProperty("id").GetString() ?? "";
            if (prefixes.Any(p => id.StartsWith(p)))
                models.Add(new { id });
        }

        return models.OrderBy(m => ((dynamic)m).id).ToList();
    }

    public async Task<List<object>> GetClaudeModelsAsync()
    {
        var apiKey = await _integrationService.GetSecretAsync("claude");
        if (string.IsNullOrEmpty(apiKey)) return new();

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await client.GetFromJsonAsync<JsonElement>("https://api.anthropic.com/v1/models?limit=100");
        var models = new List<object>();

        foreach (var model in response.GetProperty("data").EnumerateArray())
        {
            var id = model.GetProperty("id").GetString() ?? "";
            var displayName = model.TryGetProperty("display_name", out var dn) ? dn.GetString() ?? id : id;
            models.Add(new { id, displayName });
        }

        return models;
    }

    public async Task<string?> ChatAsync(string provider, string systemPrompt, string userMessage)
    {
        if (provider == "openai") return await ChatOpenAiAsync(systemPrompt, userMessage);
        if (provider == "claude") return await ChatClaudeAsync(systemPrompt, userMessage);
        return null;
    }

    private async Task<string?> ChatOpenAiAsync(string systemPrompt, string userMessage)
    {
        var apiKey = await _integrationService.GetSecretAsync("openai");
        if (string.IsNullOrEmpty(apiKey)) return null;

        var integration = await _integrationService.GetByProviderAsync("openai");
        var settings = ParseSettings(integration?.Settings);
        var model = settings.GetValueOrDefault("model", "gpt-4o");
        var maxTokens = int.TryParse(settings.GetValueOrDefault("maxTokens", "4096"), out var mt) ? mt : 4096;
        var temperature = double.TryParse(settings.GetValueOrDefault("temperature", "0.7"), out var t) ? t : 0.7;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);

        var response = await client.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMessage }
            }
        });

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }

    private async Task<string?> ChatClaudeAsync(string systemPrompt, string userMessage)
    {
        var apiKey = await _integrationService.GetSecretAsync("claude");
        if (string.IsNullOrEmpty(apiKey)) return null;

        var integration = await _integrationService.GetByProviderAsync("claude");
        var settings = ParseSettings(integration?.Settings);
        var model = settings.GetValueOrDefault("model", "claude-sonnet-4-20250514");
        var maxTokens = int.TryParse(settings.GetValueOrDefault("maxTokens", "4096"), out var mt) ? mt : 4096;
        var temperature = double.TryParse(settings.GetValueOrDefault("temperature", "1.0"), out var t) ? t : 1.0;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("x-api-key", apiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

        var response = await client.PostAsJsonAsync("https://api.anthropic.com/v1/messages", new
        {
            model,
            max_tokens = maxTokens,
            temperature,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = userMessage }
            }
        });

        if (!response.IsSuccessStatusCode) return null;

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("content")[0].GetProperty("text").GetString();
    }

    private static Dictionary<string, string> ParseSettings(string? json)
    {
        if (string.IsNullOrEmpty(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? new();
        }
        catch { return new(); }
    }
}
