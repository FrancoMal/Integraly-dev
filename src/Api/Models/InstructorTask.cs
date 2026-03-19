using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Api.Models;

[Table("InstructorTasks")]
public class InstructorTask
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    public int InstructorId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = "";

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    [MaxLength(50)]
    public string TaskType { get; set; } = "otra"; // "clase", "preparacion", "administrativa", "otra"

    public DateTime TaskDate { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal HoursWorked { get; set; }

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "pendiente"; // "pendiente", "en_progreso", "completada"

    public int? AssignedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }

    [ForeignKey("InstructorId")]
    public User? Instructor { get; set; }

    [ForeignKey("AssignedByUserId")]
    public User? AssignedByUser { get; set; }
}
