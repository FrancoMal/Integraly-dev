using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("MeliItems")]
public class MeliItem
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string MeliItemId { get; set; } = string.Empty;

    public int MeliAccountId { get; set; }

    [ForeignKey("MeliAccountId")]
    public virtual MeliAccount? MeliAccount { get; set; }

    [Required]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(50)]
    public string? CategoryId { get; set; }

    [MaxLength(500)]
    public string? CategoryPath { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? OriginalPrice { get; set; }

    [MaxLength(10)]
    public string CurrencyId { get; set; } = "ARS";

    public int AvailableQuantity { get; set; }
    public int SoldQuantity { get; set; }

    [MaxLength(50)]
    public string Status { get; set; } = "active";

    [MaxLength(20)]
    public string? Condition { get; set; }

    [MaxLength(50)]
    public string? ListingTypeId { get; set; }

    public bool FreeShipping { get; set; }

    [MaxLength(500)]
    public string? Thumbnail { get; set; }

    [MaxLength(1000)]
    public string? Permalink { get; set; }

    [MaxLength(255)]
    public string? Brand { get; set; }

    [MaxLength(255)]
    public string? Sku { get; set; }

    public bool IsCatalog { get; set; }

    public DateTime? DateCreated { get; set; }
    public DateTime? LastUpdated { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
