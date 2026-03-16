using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Bookings")]
public class Booking
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int UserId { get; set; }

    public int InstructorId { get; set; }

    public int TokenPackId { get; set; }

    public DateTime ScheduledDate { get; set; }

    public int StartHour { get; set; }

    [Required]
    [MaxLength(20)]
    public string Status { get; set; } = "confirmed";

    [MaxLength(500)]
    public string? MeetLink { get; set; }

    public DateTime? CancelledAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("InstructorId")]
    public User? Instructor { get; set; }

    [ForeignKey("TokenPackId")]
    public TokenPack? TokenPack { get; set; }
}
