using System.Text.Json;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Services;

public class MeliItemService
{
    private readonly AppDbContext _db;
    private readonly MeliAccountService _accountService;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<MeliItemService> _logger;

    public MeliItemService(AppDbContext db, MeliAccountService accountService,
        IHttpClientFactory httpClientFactory, ILogger<MeliItemService> logger)
    {
        _db = db;
        _accountService = accountService;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<List<MeliItemDto>> GetItemsAsync(int? accountId = null, string? status = null)
    {
        var query = _db.MeliItems.Include(i => i.MeliAccount).AsQueryable();
        if (accountId.HasValue) query = query.Where(i => i.MeliAccountId == accountId.Value);
        if (!string.IsNullOrEmpty(status)) query = query.Where(i => i.Status == status);

        var items = await query.OrderByDescending(i => i.LastUpdated).ToListAsync();
        return items.Select(ToDto).ToList();
    }

    public async Task<int> SyncItemsAsync()
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

                var allItemIds = new List<string>();
                string? scrollId = null;

                // Fetch all item IDs using scroll
                do
                {
                    var url = $"https://api.mercadolibre.com/users/{account.MeliUserId}/items/search?limit=100";
                    if (scrollId != null) url += $"&scroll_id={scrollId}";

                    var searchResponse = await client.GetFromJsonAsync<JsonElement>(url);
                    var ids = searchResponse.GetProperty("results").EnumerateArray()
                        .Select(x => x.GetString()!).ToList();
                    allItemIds.AddRange(ids);

                    scrollId = searchResponse.TryGetProperty("scroll_id", out var sid) ? sid.GetString() : null;
                } while (scrollId != null && allItemIds.Count < 10000);

                // Fetch details in batches of 20
                for (int i = 0; i < allItemIds.Count; i += 20)
                {
                    var batch = allItemIds.Skip(i).Take(20);
                    var idsParam = string.Join(",", batch);
                    var itemsResponse = await client.GetFromJsonAsync<JsonElement>($"https://api.mercadolibre.com/items?ids={idsParam}");

                    foreach (var wrapper in itemsResponse.EnumerateArray())
                    {
                        if (wrapper.GetProperty("code").GetInt32() != 200) continue;
                        var body = wrapper.GetProperty("body");

                        var meliItemId = body.GetProperty("id").GetString()!;
                        var existing = await _db.MeliItems.FirstOrDefaultAsync(x => x.MeliItemId == meliItemId);

                        var title = body.GetProperty("title").GetString() ?? "";
                        var price = body.GetProperty("price").GetDecimal();
                        var originalPrice = body.TryGetProperty("original_price", out var op) && op.ValueKind == JsonValueKind.Number ? op.GetDecimal() : (decimal?)null;
                        var availableQty = body.GetProperty("available_quantity").GetInt32();
                        var soldQty = body.TryGetProperty("sold_quantity", out var sq) ? sq.GetInt32() : 0;
                        var itemStatus = body.GetProperty("status").GetString() ?? "active";
                        var condition = body.TryGetProperty("condition", out var cond) ? cond.GetString() : null;
                        var listingType = body.TryGetProperty("listing_type_id", out var lt) ? lt.GetString() : null;
                        var freeShipping = body.TryGetProperty("shipping", out var ship) && ship.TryGetProperty("free_shipping", out var fs) && fs.GetBoolean();
                        var thumbnail = body.TryGetProperty("thumbnail", out var th) ? th.GetString()?.Replace("http://", "https://") : null;
                        var permalink = body.TryGetProperty("permalink", out var pl) ? pl.GetString() : null;
                        var categoryId = body.TryGetProperty("category_id", out var cat) ? cat.GetString() : null;
                        var currencyId = body.TryGetProperty("currency_id", out var cur) ? cur.GetString() ?? "ARS" : "ARS";

                        // Extract SKU and Brand from attributes
                        string? sku = null, brand = null;
                        if (body.TryGetProperty("attributes", out var attrs))
                        {
                            foreach (var attr in attrs.EnumerateArray())
                            {
                                var attrId = attr.GetProperty("id").GetString();
                                if (attrId == "SELLER_SKU")
                                    sku = attr.TryGetProperty("value_name", out var v) ? v.GetString() : null;
                                else if (attrId == "BRAND")
                                    brand = attr.TryGetProperty("value_name", out var v2) ? v2.GetString() : null;
                            }
                        }

                        var isCatalog = body.TryGetProperty("catalog_listing", out var cl) && cl.ValueKind == JsonValueKind.True;

                        if (existing is null)
                        {
                            existing = new MeliItem { MeliItemId = meliItemId, MeliAccountId = account.Id, CreatedAt = DateTime.UtcNow };
                            _db.MeliItems.Add(existing);
                        }

                        existing.Title = title;
                        existing.Price = price;
                        existing.OriginalPrice = originalPrice;
                        existing.AvailableQuantity = availableQty;
                        existing.SoldQuantity = soldQty;
                        existing.Status = itemStatus;
                        existing.Condition = condition;
                        existing.ListingTypeId = listingType;
                        existing.FreeShipping = freeShipping;
                        existing.Thumbnail = thumbnail;
                        existing.Permalink = permalink;
                        existing.CategoryId = categoryId;
                        existing.CurrencyId = currencyId;
                        existing.Sku = sku;
                        existing.Brand = brand;
                        existing.IsCatalog = isCatalog;
                        existing.LastUpdated = DateTime.UtcNow;
                        existing.UpdatedAt = DateTime.UtcNow;

                        totalSynced++;
                    }
                }

                // Mark deleted items
                var currentIds = allItemIds.ToHashSet();
                var dbItems = await _db.MeliItems.Where(x => x.MeliAccountId == account.Id && x.Status != "deleted").ToListAsync();
                foreach (var item in dbItems)
                {
                    if (!currentIds.Contains(item.MeliItemId))
                    {
                        item.Status = "deleted";
                        item.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _db.SaveChangesAsync();
                _logger.LogInformation("Synced {Count} items for MeLi account {AccountId}", totalSynced, account.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing items for MeLi account {AccountId}", account.Id);
            }
        }

        return totalSynced;
    }

    public async Task<(bool Success, string? Error)> UpdateItemAsync(string meliItemId, UpdateMeliItemRequest request)
    {
        var item = await _db.MeliItems.Include(i => i.MeliAccount).FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);
        if (item?.MeliAccount is null) return (false, "Item no encontrado");

        var token = await _accountService.GetValidTokenAsync(item.MeliAccount);
        if (token is null) return (false, "Token invalido");

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var updateBody = new Dictionary<string, object>();
        if (request.Title != null) updateBody["title"] = request.Title;
        if (request.Price.HasValue) updateBody["price"] = request.Price.Value;
        if (request.AvailableQuantity.HasValue) updateBody["available_quantity"] = request.AvailableQuantity.Value;
        if (request.Status != null) updateBody["status"] = request.Status;

        var response = await client.PutAsJsonAsync($"https://api.mercadolibre.com/items/{meliItemId}", updateBody);
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            return (false, $"Error de MercadoLibre: {error}");
        }

        // Update local DB
        if (request.Title != null) item.Title = request.Title;
        if (request.Price.HasValue) item.Price = request.Price.Value;
        if (request.AvailableQuantity.HasValue) item.AvailableQuantity = request.AvailableQuantity.Value;
        if (request.Status != null) item.Status = request.Status;
        item.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return (true, null);
    }

    public async Task<object?> GetItemDetailsAsync(string meliItemId)
    {
        var item = await _db.MeliItems.Include(i => i.MeliAccount).FirstOrDefaultAsync(i => i.MeliItemId == meliItemId);
        if (item?.MeliAccount is null) return null;

        var token = await _accountService.GetValidTokenAsync(item.MeliAccount);
        if (token is null) return null;

        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var itemTask = client.GetFromJsonAsync<JsonElement>($"https://api.mercadolibre.com/items/{meliItemId}");
        var descTask = client.GetFromJsonAsync<JsonElement>($"https://api.mercadolibre.com/items/{meliItemId}/description");

        await Task.WhenAll(itemTask, descTask);

        var itemData = await itemTask;
        var descData = await descTask;

        var pictures = new List<object>();
        if (itemData.TryGetProperty("pictures", out var pics))
        {
            foreach (var pic in pics.EnumerateArray())
            {
                pictures.Add(new
                {
                    id = pic.GetProperty("id").GetString(),
                    url = pic.TryGetProperty("secure_url", out var su) ? su.GetString() : pic.GetProperty("url").GetString()
                });
            }
        }

        return new
        {
            pictures,
            description = descData.TryGetProperty("plain_text", out var pt) ? pt.GetString() : ""
        };
    }

    private static MeliItemDto ToDto(MeliItem i) => new(
        i.Id, i.MeliItemId, i.MeliAccountId, i.MeliAccount?.Nickname,
        i.Title, i.CategoryId, i.Price, i.OriginalPrice, i.CurrencyId,
        i.AvailableQuantity, i.SoldQuantity, i.Status, i.Condition,
        i.ListingTypeId, i.FreeShipping, i.Thumbnail, i.Permalink,
        i.Brand, i.Sku, i.IsCatalog, i.DateCreated, i.LastUpdated
    );
}
