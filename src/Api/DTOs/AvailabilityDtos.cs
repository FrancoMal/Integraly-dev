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

public record WeekAvailabilityDto(int Id, int InstructorId, DateTime Date, int StartHour, bool IsActive);

public class WeekSlotRequest
{
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
    public bool IsActive { get; set; }
}

public class SetWeekAvailabilityRequest
{
    public DateTime WeekStart { get; set; }
    public List<WeekSlotRequest> Slots { get; set; } = new();
}

public record AdminToggleAvailabilityRequest(
    [Required] int InstructorId,
    [Required] DateTime Date,
    [Required][Range(0, 23)] int StartHour,
    [Required] bool IsActive
);

public class AdminBulkToggleSlot
{
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
}

public class AdminBulkToggleRequest
{
    public int InstructorId { get; set; }
    public bool IsActive { get; set; }
    public List<AdminBulkToggleSlot> Slots { get; set; } = new();
}
