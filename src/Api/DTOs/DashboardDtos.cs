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
    int UpcomingBookings,
    int BookedClasses,
    int UsedClasses
);

public record StudentSummaryDto(
    int UserId,
    string Name,
    string Username,
    int TotalClasses,
    int CompletedClasses,
    int ReservedClasses,
    int PendingClasses
);
