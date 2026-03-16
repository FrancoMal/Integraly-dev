using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record AvailabilityDto(
    int Id,
    int InstructorId,
    int DayOfWeek,
    int StartHour,
    bool IsActive
);

public record SetAvailabilityRequest(
    [Required][Range(0, 6)] int DayOfWeek,
    [Required][Range(0, 23)] int StartHour,
    [Required] bool IsActive
);
