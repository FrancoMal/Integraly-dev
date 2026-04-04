using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Integrations")]
public class Integration
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Provider { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? AppId { get; set; }

    [MaxLength(255)]
    public string? AppSecret { get; set; }

    [MaxLength(500)]
    public string? RedirectUrl { get; set; }

    public string? Settings { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
