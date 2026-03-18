using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly TokenPackService _tokenPackService;

    public DashboardController(AppDbContext db, TokenPackService tokenPackService)
    {
        _db = db;
        _tokenPackService = tokenPackService;
    }

    [HttpGet]
    public async Task<IActionResult> GetDashboard()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        switch (role)
        {
            case "admin":
                return Ok(await GetAdminDashboard());
            case "instructor":
                return Ok(await GetInstructorDashboard(userId.Value));
            default:
                return Ok(await GetUserDashboard(userId.Value));
        }
    }

    private async Task<AdminDashboardDto> GetAdminDashboard()
    {
        var totalUsers = await _db.Users.CountAsync();
        var totalInstructors = await _db.Users
            .Include(u => u.RoleNav)
            .CountAsync(u => u.RoleNav != null && u.RoleNav.Name == "instructor");
        var totalStudents = await _db.Users
            .Include(u => u.RoleNav)
            .CountAsync(u => u.RoleNav != null && u.RoleNav.Name == "usuario");
        var totalBookings = await _db.Bookings.CountAsync();
        var activeBookings = await _db.Bookings.CountAsync(b => b.Status == "confirmed");

        return new AdminDashboardDto(totalUsers, totalInstructors, totalStudents, totalBookings, activeBookings);
    }

    private async Task<InstructorDashboardDto> GetInstructorDashboard(int instructorId)
    {
        var totalBookings = await _db.Bookings.CountAsync(b => b.InstructorId == instructorId);
        var upcomingBookings = await _db.Bookings.CountAsync(b =>
            b.InstructorId == instructorId
            && b.Status == "confirmed"
            && b.ScheduledDate >= DateTime.UtcNow.Date);

        return new InstructorDashboardDto(totalBookings, upcomingBookings);
    }

    private async Task<UserDashboardDto> GetUserDashboard(int userId)
    {
        var remainingTokens = await _tokenPackService.GetAvailableTokensAsync(userId);
        var upcomingBookings = await _db.Bookings.CountAsync(b =>
            b.UserId == userId
            && b.Status == "confirmed"
            && b.ScheduledDate >= DateTime.UtcNow.Date);
        var bookedClasses = await _db.Bookings.CountAsync(b =>
            b.UserId == userId
            && b.Status == "confirmed");
        var usedClasses = await _db.Bookings.CountAsync(b =>
            b.UserId == userId
            && b.Status == "completed");

        return new UserDashboardDto(remainingTokens, upcomingBookings, bookedClasses, usedClasses);
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }
}
