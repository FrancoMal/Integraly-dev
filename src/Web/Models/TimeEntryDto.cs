namespace Web.Models;

public class TimeEntryDto
{
    public int Id { get; set; }
    public int InstructorId { get; set; }
    public string InstructorName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateTimeEntryRequest
{
    public DateTime Date { get; set; }
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public string? Description { get; set; }
}

public class UpdateTimeEntryRequest
{
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public string? Description { get; set; }
}

public class InstructorListItemDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}
