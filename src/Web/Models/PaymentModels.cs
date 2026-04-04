namespace Web.Models;

public class PaymentPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Classes { get; set; }
    public decimal Price { get; set; }
    public decimal PriceUSD { get; set; }
    public string Currency { get; set; } = "ARS";
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; }
    public bool Active { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreatePaymentResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public int PaymentId { get; set; }
}

public class PaymentDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int Classes { get; set; }
    public int PlanId { get; set; }
    public decimal Amount { get; set; }
    public string? Currency { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentProvider { get; set; }
    public string? TransferReceiptUrl { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class TransferPaymentResponse
{
    public string? CheckoutUrl { get; set; }
    public int PaymentId { get; set; }
    public bool Transfer { get; set; }
    public decimal Amount { get; set; }
    public string Cvu { get; set; } = string.Empty;
    public string Alias { get; set; } = string.Empty;
    public string Titular { get; set; } = string.Empty;
}

public class CountryDetectResult
{
    public string Country { get; set; } = "AR";
    public string Provider { get; set; } = "mercadopago";
}

public class PlanRequestDto
{
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Classes { get; set; }
    public decimal Price { get; set; }
    public decimal PriceUSD { get; set; }
    public string? Currency { get; set; } = "ARS";
    public bool Active { get; set; } = true;
    public int DisplayOrder { get; set; }
}
