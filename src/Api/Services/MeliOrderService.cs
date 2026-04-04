using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MeliOrderService
{
    private readonly AppDbContext _db;
    private readonly MeliAccountService _accountService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MeliOrderService> _logger;

    public MeliOrderService(AppDbContext db, MeliAccountService accountService,
        IHttpClientFactory httpClientFactory, ILogger<MeliOrderService> logger)
    {
        _db = db;
        _accountService = accountService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<MeliOrderDto>> GetOrdersAsync(DateTime? from = null, DateTime? to = null, int? accountId = null)
    {
        var query = _db.MeliOrders.Include(o => o.MeliAccount).AsQueryable();
        if (from.HasValue) query = query.Where(o => o.DateCreated >= from.Value);
        if (to.HasValue) query = query.Where(o => o.DateCreated <= to.Value);
        if (accountId.HasValue) query = query.Where(o => o.MeliAccountId == accountId.Value);

        var orders = await query.OrderByDescending(o => o.DateCreated).ToListAsync();
        return orders.Select(ToDto).ToList();
    }

    public async Task<int> SyncOrdersAsync(DateTime from, DateTime to)
    {
        var accounts = await _db.MeliAccounts.ToListAsync();
        var totalSynced = 0;

        foreach (var account in accounts)
        {
            try
            {
                var token = await _accountService.GetValidTokenAsync(account);
                if (token is null) continue;

                var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

                var offset = 0;
                var hasMore = true;

                while (hasMore)
                {
                    var fromStr = from.ToString("yyyy-MM-ddTHH:mm:ss.000-00:00");
                    var toStr = to.ToString("yyyy-MM-ddTHH:mm:ss.000-00:00");
                    var url = $"https://api.mercadolibre.com/orders/search?seller={account.MeliUserId}&order.date_created.from={fromStr}&order.date_created.to={toStr}&offset={offset}&limit=50";

                    var response = await client.GetFromJsonAsync<JsonElement>(url);
                    var results = response.GetProperty("results");

                    foreach (var order in results.EnumerateArray())
                    {
                        var meliOrderId = order.GetProperty("id").GetInt64();
                        var existing = await _db.MeliOrders.FirstOrDefaultAsync(o => o.MeliOrderId == meliOrderId && o.MeliAccountId == account.Id);

                        var status = order.GetProperty("status").GetString() ?? "";
                        var dateCreated = order.GetProperty("date_created").GetDateTime();
                        var dateClosed = order.TryGetProperty("date_closed", out var dc) && dc.ValueKind != JsonValueKind.Null ? dc.GetDateTime() : (DateTime?)null;
                        var totalAmount = order.GetProperty("total_amount").GetDecimal();
                        var currencyId = order.GetProperty("currency_id").GetString() ?? "ARS";

                        var buyer = order.GetProperty("buyer");
                        var buyerId = buyer.GetProperty("id").GetInt64();
                        var buyerNickname = buyer.GetProperty("nickname").GetString() ?? "";

                        var orderItems = order.GetProperty("order_items");
                        var firstItem = orderItems.EnumerateArray().FirstOrDefault();
                        var itemId = "";
                        var itemTitle = "";
                        var quantity = 1;
                        var unitPrice = totalAmount;

                        if (firstItem.ValueKind != JsonValueKind.Undefined)
                        {
                            var item = firstItem.GetProperty("item");
                            itemId = item.GetProperty("id").GetString() ?? "";
                            itemTitle = item.GetProperty("title").GetString() ?? "";
                            quantity = firstItem.GetProperty("quantity").GetInt32();
                            unitPrice = firstItem.GetProperty("unit_price").GetDecimal();
                        }

                        long? shippingId = null;
                        string? shippingStatus = null;
                        if (order.TryGetProperty("shipping", out var shipping) && shipping.ValueKind == JsonValueKind.Object)
                        {
                            shippingId = shipping.TryGetProperty("id", out var sid) && sid.ValueKind == JsonValueKind.Number ? sid.GetInt64() : null;

                            if (shippingId.HasValue && shippingId.Value > 0)
                            {
                                try
                                {
                                    var shipResponse = await client.GetFromJsonAsync<JsonElement>($"https://api.mercadolibre.com/shipments/{shippingId}");
                                    shippingStatus = shipResponse.TryGetProperty("status", out var ss) ? ss.GetString() : null;
                                }
                                catch { /* ignore shipping errors */ }
                            }
                        }

                        var packId = order.TryGetProperty("pack_id", out var pid) && pid.ValueKind == JsonValueKind.Number ? pid.GetInt64() : (long?)null;

                        if (existing is null)
                        {
                            existing = new MeliOrder
                            {
                                MeliOrderId = meliOrderId,
                                MeliAccountId = account.Id,
                                CreatedAt = DateTime.UtcNow
                            };
                            _db.MeliOrders.Add(existing);
                        }

                        existing.Status = status;
                        existing.DateCreated = dateCreated;
                        existing.DateClosed = dateClosed;
                        existing.TotalAmount = totalAmount;
                        existing.CurrencyId = currencyId;
                        existing.BuyerId = buyerId;
                        existing.BuyerNickname = buyerNickname;
                        existing.ItemId = itemId;
                        existing.ItemTitle = itemTitle;
                        existing.Quantity = quantity;
                        existing.UnitPrice = unitPrice;
                        existing.ShippingId = shippingId;
                        existing.PackId = packId;
                        existing.ShippingStatus = shippingStatus;
                        existing.UpdatedAt = DateTime.UtcNow;

                        totalSynced++;
                    }

                    await _db.SaveChangesAsync();

                    var total = response.TryGetProperty("paging", out var paging) ? paging.GetProperty("total").GetInt32() : 0;
                    offset += 50;
                    hasMore = offset < total;
                }

                _logger.LogInformation("Synced orders for MeLi account {AccountId}", account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing orders for MeLi account {AccountId}", account.Id);
            }
        }

        return totalSynced;
    }

    public async Task<object?> GetOrderDetailAsync(long meliOrderId)
    {
        var order = await _db.MeliOrders.Include(o => o.MeliAccount).FirstOrDefaultAsync(o => o.MeliOrderId == meliOrderId);
        if (order?.MeliAccount is null) return null;

        var token = await _accountService.GetValidTokenAsync(order.MeliAccount);
        if (token is null) return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetFromJsonAsync<JsonElement>($"https://api.mercadolibre.com/orders/{meliOrderId}");
        return response;
    }

    private static MeliOrderDto ToDto(MeliOrder o) => new(
        o.Id, o.MeliOrderId, o.MeliAccountId, o.MeliAccount?.Nickname,
        o.Status, o.DateCreated, o.DateClosed, o.TotalAmount, o.CurrencyId,
        o.BuyerId, o.BuyerNickname, o.ItemId, o.ItemTitle, o.Quantity,
        o.UnitPrice, o.ShippingId, o.PackId, o.ShippingStatus
    );
}
