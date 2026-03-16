using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("TokenPacks")]
public class TokenPack
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int TotalTokens { get; set; }

    public int RemainingTokens { get; set; }

    public int CreatedBy { get; set; }

    [MaxLength(500)]
    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("CreatedBy")]
    public User? CreatedByUser { get; set; }
}
