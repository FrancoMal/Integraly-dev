using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Invitations")]
public class Invitation
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    public int RoleId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Token { get; set; } = string.Empty;

    public int CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UsedAt { get; set; }

    public DateTime ExpiresAt { get; set; }

    [ForeignKey("RoleId")]
    public Role? Role { get; set; }

    [ForeignKey("CreatedBy")]
    public User? CreatedByUser { get; set; }
}
