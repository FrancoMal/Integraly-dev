using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record InstructorTaskDto(
    int Id,
    int InstructorId,
    string InstructorName,
    string Title,
    string? Description,
    string TaskType,
    DateTime TaskDate,
    int StartHour,
    int EndHour,
    decimal HoursWorked,
    string Status,
    int? AssignedByUserId,
    string? AssignedByName,
    DateTime CreatedAt,
    DateTime? CompletedAt
);

public record CreateInstructorTaskRequest(
    [Required] int InstructorId,
    [Required] string Title,
    string? Description,
    string TaskType = "otra",
    [Required] DateTime TaskDate = default,
    int StartHour = 8,
    int EndHour = 9,
    [Required] decimal HoursWorked = 0
);

public record UpdateInstructorTaskRequest(
    string? Title,
    string? Description,
    string? TaskType,
    DateTime? TaskDate,
    int? StartHour,
    int? EndHour,
    decimal? HoursWorked,
    string? Status
);
