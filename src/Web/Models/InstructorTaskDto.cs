namespace Web.Models;

public class InstructorTaskDto
{
    public int Id { get; set; }
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskType { get; set; } = string.Empty;
    public DateTime TaskDate { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public decimal HoursWorked { get; set; }
    public string Status { get; set; } = string.Empty;
    public int? AssignedByUserId { get; set; }
    public string? AssignedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class CreateInstructorTaskRequest
{
    public int InstructorId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TaskType { get; set; } = "otra";
    public string Status { get; set; } = "asignada";
    public DateTime TaskDate { get; set; }
    public int StartHour { get; set; } = 8;
    public int EndHour { get; set; } = 9;
    public decimal HoursWorked { get; set; }
}

public class UpdateInstructorTaskRequest
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? TaskType { get; set; }
    public DateTime? TaskDate { get; set; }
    public int? StartHour { get; set; }
    public int? EndHour { get; set; }
    public decimal? HoursWorked { get; set; }
    public string? Status { get; set; }
}
