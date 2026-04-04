using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("MeliOrders")]
public class MeliOrder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public long MeliOrderId { get; set; }

    public int MeliAccountId { get; set; }

    [ForeignKey("MeliAccountId")]
    public virtual MeliAccount? MeliAccount { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = string.Empty;

    public DateTime DateCreated { get; set; }
    public DateTime? DateClosed { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [MaxLength(10)]
    public string CurrencyId { get; set; } = "ARS";

    public long BuyerId { get; set; }

    [MaxLength(255)]
    public string BuyerNickname { get; set; } = string.Empty;

    [MaxLength(50)]
    public string ItemId { get; set; } = string.Empty;

    [MaxLength(500)]
    public string ItemTitle { get; set; } = string.Empty;

    public int Quantity { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; set; }

    public long? ShippingId { get; set; }
    public long? PackId { get; set; }

    [MaxLength(50)]
    public string? ShippingStatus { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
