using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Api.Models;

namespace Api.Services;

public class PayPalService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _configuration;
    private readonly ILogger<PayPalService> _logger;

    public PayPalService(
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration,
        ILogger<PayPalService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetClientId()
    {
        return _configuration["PayPal:ClientId"]
            ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_ID")
            ?? throw new InvalidOperationException("PAYPAL_CLIENT_ID not configured");
    }

    private string GetClientSecret()
    {
        return _configuration["PayPal:ClientSecret"]
            ?? Environment.GetEnvironmentVariable("PAYPAL_CLIENT_SECRET")
            ?? throw new InvalidOperationException("PAYPAL_CLIENT_SECRET not configured");
    }

    private string GetBaseApiUrl()
    {
        var mode = _configuration["PayPal:Mode"]
            ?? Environment.GetEnvironmentVariable("PAYPAL_MODE")
            ?? "sandbox";

        return mode == "live"
            ? "https://api-m.paypal.com"
            : "https://api-m.sandbox.paypal.com";
    }

    private string GetAppBaseUrl()
    {
        return _configuration["PayPal:BaseUrl"]
            ?? _configuration["MercadoPago:BaseUrl"]
            ?? Environment.GetEnvironmentVariable("APP_BASE_URL")
            ?? "https://integraly.dev";
    }

    /// <summary>
    /// Get OAuth2 access token from PayPal using client credentials
    /// </summary>
    private async Task<string?> GetAccessTokenAsync()
    {
        var clientId = GetClientId();
        var clientSecret = GetClientSecret();
        var baseUrl = GetBaseApiUrl();

        var client = _httpClientFactory.CreateClient();
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var content = new StringContent("grant_type=client_credentials", Encoding.UTF8, "application/x-www-form-urlencoded");

        try
        {
            var response = await client.PostAsync($"{baseUrl}/v1/oauth2/token", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal GetAccessToken failed: {Status} {Body}", response.StatusCode, responseBody);
                return null;
            }

            using var doc = JsonDocument.Parse(responseBody);
            return doc.RootElement.GetProperty("access_token").GetString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting PayPal access token");
            return null;
        }
    }

    /// <summary>
    /// Create a PayPal order and return the approval URL
    /// </summary>
    public async Task<(string? approvalUrl, string? orderId)> CreateOrder(Payment payment, PaymentPlan plan, string userEmail)
    {
        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null) return (null, null);

        var baseUrl = GetBaseApiUrl();
        var appBaseUrl = GetAppBaseUrl();

        // Use payment.Amount and payment.Currency (already set to USD by controller)
        var amountStr = payment.Amount.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
        var currencyCode = payment.Currency == "ARS" ? "USD" : payment.Currency;

        var order = new
        {
            intent = "CAPTURE",
            purchase_units = new[]
            {
                new
                {
                    reference_id = payment.Id.ToString(),
                    description = plan.Description ?? $"{plan.Classes} clase(s) particular(es)",
                    amount = new
                    {
                        currency_code = currencyCode,
                        value = amountStr,
                        breakdown = new
                        {
                            item_total = new
                            {
                                currency_code = currencyCode,
                                value = amountStr
                            }
                        }
                    },
                    items = new[]
                    {
                        new
                        {
                            name = plan.Name,
                            description = plan.Description ?? $"{plan.Classes} clase(s)",
                            quantity = "1",
                            unit_amount = new
                            {
                                currency_code = currencyCode,
                                value = amountStr
                            }
                        }
                    }
                }
            },
            payment_source = new
            {
                paypal = new
                {
                    experience_context = new
                    {
                        payment_method_preference = "IMMEDIATE_PAYMENT_REQUIRED",
                        brand_name = "Integraly",
                        locale = "es-AR",
                        landing_page = "LOGIN",
                        user_action = "PAY_NOW",
                        return_url = $"{appBaseUrl}/api/payments/paypal/capture?paymentId={payment.Id}",
                        cancel_url = $"{appBaseUrl}/panel/pago-fallido"
                    }
                }
            }
        };

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var json = JsonSerializer.Serialize(order);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal CreateOrder failed: {Status} {Body}", response.StatusCode, responseBody);
                return (null, null);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var orderId = doc.RootElement.GetProperty("id").GetString();

            // Find the payer-action link (where user approves the payment)
            string? approvalUrl = null;
            if (doc.RootElement.TryGetProperty("links", out var links))
            {
                foreach (var link in links.EnumerateArray())
                {
                    var rel = link.GetProperty("rel").GetString();
                    if (rel == "payer-action")
                    {
                        approvalUrl = link.GetProperty("href").GetString();
                        break;
                    }
                }
            }

            _logger.LogInformation("PayPal order created for payment {PaymentId}: OrderId={OrderId}, ApprovalUrl={Url}",
                payment.Id, orderId, approvalUrl);

            return (approvalUrl, orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating PayPal order for payment {PaymentId}", payment.Id);
            return (null, null);
        }
    }

    /// <summary>
    /// Capture a PayPal order after user approval
    /// </summary>
    public async Task<(string? status, string? captureId)> CaptureOrder(string paypalOrderId)
    {
        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null) return (null, null);

        var baseUrl = GetBaseApiUrl();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var content = new StringContent("", Encoding.UTF8, "application/json");

        try
        {
            var response = await client.PostAsync($"{baseUrl}/v2/checkout/orders/{paypalOrderId}/capture", content);
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal CaptureOrder failed: {Status} {Body}", response.StatusCode, responseBody);
                return (null, null);
            }

            using var doc = JsonDocument.Parse(responseBody);
            var status = doc.RootElement.GetProperty("status").GetString();

            string? captureId = null;
            if (doc.RootElement.TryGetProperty("purchase_units", out var units))
            {
                var firstUnit = units[0];
                if (firstUnit.TryGetProperty("payments", out var payments)
                    && payments.TryGetProperty("captures", out var captures)
                    && captures.GetArrayLength() > 0)
                {
                    captureId = captures[0].GetProperty("id").GetString();
                }
            }

            _logger.LogInformation("PayPal order {OrderId} captured: status={Status}, captureId={CaptureId}",
                paypalOrderId, status, captureId);

            return (status, captureId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error capturing PayPal order {OrderId}", paypalOrderId);
            return (null, null);
        }
    }

    /// <summary>
    /// Get order details from PayPal
    /// </summary>
    public async Task<JsonDocument?> GetOrderDetails(string paypalOrderId)
    {
        var accessToken = await GetAccessTokenAsync();
        if (accessToken is null) return null;

        var baseUrl = GetBaseApiUrl();
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await client.GetAsync($"{baseUrl}/v2/checkout/orders/{paypalOrderId}");
            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("PayPal GetOrderDetails failed: {Status} {Body}", response.StatusCode, responseBody);
                return null;
            }

            return JsonDocument.Parse(responseBody);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching PayPal order {OrderId}", paypalOrderId);
            return null;
        }
    }
}
