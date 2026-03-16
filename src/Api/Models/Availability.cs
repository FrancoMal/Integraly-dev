using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("Availabilities")]
public class Availability
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int InstructorId { get; set; }

    public int DayOfWeek { get; set; }

    public int StartHour { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [ForeignKey("InstructorId")]
    public User? Instructor { get; set; }
}
