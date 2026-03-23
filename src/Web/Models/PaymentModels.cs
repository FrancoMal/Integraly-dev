namespace Web.Models;

public class PaymentPlanDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Classes { get; set; }
    public decimal Price { get; set; }
    public bool IsPopular { get; set; }
    public bool IsActive { get; set; }
}

public class CreatePaymentResponse
{
    public string CheckoutUrl { get; set; } = string.Empty;
    public string PaymentId { get; set; } = string.Empty;
}

public class PaymentDto
{
    public int Id { get; set; }
    public string PlanName { get; set; } = string.Empty;
    public int Classes { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
