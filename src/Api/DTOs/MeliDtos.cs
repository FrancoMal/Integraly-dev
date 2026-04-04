namespace Api.DTOs;

public record MeliAccountDto(
    int Id,
    long MeliUserId,
    string Nickname,
    string? Email,
    bool TokenValid,
    DateTime TokenExpiresAt,
    DateTime CreatedAt
);

public record MeliCallbackRequest(string Code);

public record MeliItemDto(
    int Id,
    string MeliItemId,
    int MeliAccountId,
    string? AccountNickname,
    string Title,
    string? CategoryId,
    decimal Price,
    decimal? OriginalPrice,
    string CurrencyId,
    int AvailableQuantity,
    int SoldQuantity,
    string Status,
    string? Condition,
    string? ListingTypeId,
    bool FreeShipping,
    string? Thumbnail,
    string? Permalink,
    string? Brand,
    string? Sku,
    bool IsCatalog,
    DateTime? DateCreated,
    DateTime? LastUpdated
);

public record UpdateMeliItemRequest(
    string? Title,
    decimal? Price,
    int? AvailableQuantity,
    string? Status
);

public record MeliOrderDto(
    int Id,
    long MeliOrderId,
    int MeliAccountId,
    string? AccountNickname,
    string Status,
    DateTime DateCreated,
    DateTime? DateClosed,
    decimal TotalAmount,
    string CurrencyId,
    long BuyerId,
    string BuyerNickname,
    string ItemId,
    string ItemTitle,
    int Quantity,
    decimal UnitPrice,
    long? ShippingId,
    long? PackId,
    string? ShippingStatus
);
