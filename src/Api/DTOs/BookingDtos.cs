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
    string? UserNotes,
    string? AdminNotes,
    DateTime CreatedAt
);

public record CreateBookingRequest(
    [Required] int InstructorId,
    [Required] DateTime ScheduledDate,
    [Required][Range(0, 23)] int StartHour,
    string? UserNotes
);

public record AvailableSlotDto(
    int StartHour,
    bool IsAvailable
);

public record AdminCreateBookingRequest(
    [Required] int UserId,
    [Required] int InstructorId,
    [Required] DateTime ScheduledDate,
    [Required][Range(0, 23)] int StartHour,
    string? UserNotes
);

public class UpdateBookingNotesRequest
{
    public string? UserNotes { get; set; }
    public string? AdminNotes { get; set; }
}
