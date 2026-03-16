namespace Web.Models;

public class AvailabilityDto
{
    public int Id { get; set; }
    public int InstructorId { get; set; }
    public int DayOfWeek { get; set; }
    public int StartHour { get; set; }
    public bool IsActive { get; set; }
}

public class SetAvailabilityRequest
{
    public int DayOfWeek { get; set; }
    public int StartHour { get; set; }
    public bool IsActive { get; set; }
}
