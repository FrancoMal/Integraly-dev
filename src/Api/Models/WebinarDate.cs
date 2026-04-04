using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("WebinarDates")]
public class WebinarDate
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    [MaxLength(500)]
    public string? MeetingLink { get; set; }

    [MaxLength(500)]
    public string? InviteSubject { get; set; }

    public string? InviteMessage { get; set; }

    public bool SendByEmail { get; set; }

    public bool SendByWhatsapp { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
