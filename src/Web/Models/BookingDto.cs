namespace Web.Models;

public class BookingDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public DateTime ScheduledDate { get; set; }
    public int StartHour { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? MeetLink { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateBookingRequest
{
    public int InstructorId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public int StartHour { get; set; }
}

public class AvailableSlotDto
{
    public int StartHour { get; set; }
    public bool IsAvailable { get; set; }
}

public class AdminCreateBookingRequest
{
    public int UserId { get; set; }
    public int InstructorId { get; set; }
    public DateTime ScheduledDate { get; set; }
    public int StartHour { get; set; }
}
