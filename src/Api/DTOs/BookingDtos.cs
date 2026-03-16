using System.ComponentModel.DataAnnotations;

namespace Api.DTOs;

public record BookingDto(
    int Id,
    int UserId,
    string UserName,
    int InstructorId,
    string InstructorName,
    DateTime ScheduledDate,
    int StartHour,
    string Status,
    string? MeetLink,
    DateTime CreatedAt
);

public record CreateBookingRequest(
    [Required] int InstructorId,
    [Required] DateTime ScheduledDate,
    [Required][Range(0, 23)] int StartHour
);

public record AvailableSlotDto(
    int StartHour,
    bool IsAvailable
);
