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
}
