using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Comprobantes")]
public class Comprobante
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(10)]
    public string Categoria { get; set; } = string.Empty;

    public DateTime Fecha { get; set; }

    [Required]
    [MaxLength(100)]
    public string Tipo { get; set; } = string.Empty;

    public int PuntoDeVenta { get; set; }
    public long NumeroDesde { get; set; }
    public long NumeroHasta { get; set; }
    public long? CodAutorizacion { get; set; }

    [MaxLength(20)]
    public string? ContraparteTipoDoc { get; set; }

    public long? ContraparteNroDoc { get; set; }

    [MaxLength(300)]
    public string? ContraparteDenominacion { get; set; }

    [Column(TypeName = "decimal(18,6)")]
    public decimal TipoCambio { get; set; } = 1;

    [MaxLength(10)]
    public string Moneda { get; set; } = "$";

    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva0 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Iva25 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva25 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Iva5 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva5 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Iva105 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva105 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Iva21 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva21 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? Iva27 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NetoGravIva27 { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PercIva { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PercOtrosImp { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PercIIBB { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? PercImpMuni { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ImpInterno { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? NoGravado { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? OtrosTributos { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal? ImporteTotal { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
