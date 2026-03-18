using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record TimeEntryDto(
    int Id,
    int InstructorId,
    string InstructorName,
    DateTime Date,
    int StartHour,
    int EndHour,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt
);

public record CreateTimeEntryRequest(
    [Required] DateTime Date,
    [Required][Range(0, 23)] int StartHour,
    [Required][Range(1, 24)] int EndHour,
    string? Description
);

public record UpdateTimeEntryRequest(
    [Required][Range(0, 23)] int StartHour,
    [Required][Range(1, 24)] int EndHour,
    string? Description
);
