namespace Web.Models;

public class AdminDashboardDto
{
    public int TotalUsers { get; set; }
    public int TotalInstructors { get; set; }
    public int TotalStudents { get; set; }
    public int TotalBookings { get; set; }
    public int ActiveBookings { get; set; }
}

public class InstructorDashboardDto
{
    public int TotalBookings { get; set; }
    public int UpcomingBookings { get; set; }
}

public class UserDashboardDto
{
    public int RemainingTokens { get; set; }
    public int UpcomingBookings { get; set; }
    public int BookedClasses { get; set; }
    public int UsedClasses { get; set; }
}

public class StudentSummaryDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public int TotalClasses { get; set; }
    public int CompletedClasses { get; set; }
    public int ReservedClasses { get; set; }
    public int PendingClasses { get; set; }
}
