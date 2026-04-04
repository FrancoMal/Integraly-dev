using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Payments")]
public class Payment
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int PaymentPlanId { get; set; }

    [Column(TypeName = "decimal(10,2)")]
    public decimal Amount { get; set; }

    [MaxLength(10)]
    public string Currency { get; set; } = "ARS";

    [MaxLength(50)]
    public string Status { get; set; } = "pending";

    [MaxLength(20)]
    public string PaymentProvider { get; set; } = "mercadopago";

    [MaxLength(200)]
    public string? MercadoPagoOrderId { get; set; }

    [MaxLength(200)]
    public string? MercadoPagoPaymentId { get; set; }

    [MaxLength(200)]
    public string? PayPalOrderId { get; set; }

    [MaxLength(200)]
    public string? PayPalCaptureId { get; set; }

    public int? TokenPackId { get; set; }

    [MaxLength(500)]
    public string? TransferReceiptUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ApprovedAt { get; set; }

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("PaymentPlanId")]
    public PaymentPlan? PaymentPlan { get; set; }

    [ForeignKey("TokenPackId")]
    public TokenPack? TokenPack { get; set; }
}
