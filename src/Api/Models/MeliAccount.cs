using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("MeliAccounts")]
public class MeliAccount
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public long MeliUserId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Nickname { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? Email { get; set; }

    [Required]
    public string AccessToken { get; set; } = string.Empty;

    public string? RefreshToken { get; set; }

    public DateTime TokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
