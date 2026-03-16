namespace Api.DTOs;

public record AdminDashboardDto(
    int TotalUsers,
    int TotalInstructors,
    int TotalStudents,
    int TotalBookings,
    int ActiveBookings
);

public record InstructorDashboardDto(
    int TotalBookings,
    int UpcomingBookings
);

public record UserDashboardDto(
    int RemainingTokens,
    int UpcomingBookings
);
