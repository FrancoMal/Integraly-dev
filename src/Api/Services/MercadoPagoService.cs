using System.Text;
using System.Text.Json;
using Api.Models;

namespace Api.Services;

public class MercadoPagoService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MercadoPagoService> _logger;

    public MercadoPagoService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<MercadoPagoService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetAccessToken()
    {
        return _configuration["MercadoPago:AccessToken"]
            ?? Environment.GetEnvironmentVariable("MP_ACCESS_TOKEN")
            ?? throw new InvalidOperationException("MP_ACCESS_TOKEN not configured");
    }

    private string GetBaseUrl()
    {
        return _configuration["MercadoPago:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("APP_BASE_URL")
            ?? "https://integraly.dev";
    }

    public async Task<string?> CreatePreference(Payment payment, PaymentPlan plan, string userEmail)
    {
        var accessToken = GetAccessToken();
        var baseUrl = GetBaseUrl();

        var preference = new
        {
            items = new[]
            {
                new
                {
                    title = plan.Name,
                    description = plan.Description ?? $"{plan.Classes} clase(s) particular(es)",
                    quantity = 1,
                    unit_price = payment.Amount,
                    currency_id = payment.Currency
                }
            },
            payer = new
            {
                email = userEmail
            },
            back_urls = new
            {
                success = $"{baseUrl}/panel/pago-exitoso",
                failure = $"{baseUrl}/panel/pago-fallido",
                pending = $"{baseUrl}/panel/pago-pendiente"
            },
            auto_return = "approved",
            external_reference = payment.Id.ToString(),
            notification_url = $"{baseUrl}/api/payments/webhook"
        };

        var client = _httpClientFactory.CreateClient();
        var json = JsonSerializer.Serialize(preference);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.PostAsync("https://api.mercadopago.com/checkout/preferences", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MercadoPago CreatePreference failed: {Status} {Body}",
                    response.StatusCode, responseBody);
                return null;
            }

            using var doc = JsonDocument.Parse(responseBody);
            var initPoint = doc.RootElement.GetProperty("init_point").GetString();
            _logger.LogInformation("MercadoPago preference created for payment {PaymentId}: {InitPoint}",
                payment.Id, initPoint);
            return initPoint;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MercadoPago preference for payment {PaymentId}", payment.Id);
            return null;
        }
    }

    public async Task<JsonDocument?> GetPaymentInfo(string mercadoPagoPaymentId)
    {
        var accessToken = GetAccessToken();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.GetAsync($"https://api.mercadopago.com/v1/payments/{mercadoPagoPaymentId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("MercadoPago GetPayment failed: {Status} {Body}",
                    response.StatusCode, responseBody);
                return null;
            }

            return JsonDocument.Parse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching MercadoPago payment {PaymentId}", mercadoPagoPaymentId);
            return null;
        }
    }
}
