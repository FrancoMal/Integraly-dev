using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly AuditLogService _auditLogService;

    public BookingsController(BookingService bookingService, AuditLogService auditLogService)
    {
        _bookingService = bookingService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var bookings = await _bookingService.GetAllAsync();
        return Ok(bookings);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var role = User.FindFirst(ClaimTypes.Role)?.Value;

        List<BookingDto> bookings;
        if (role == "instructor")
        {
            bookings = await _bookingService.GetByInstructorIdAsync(userId.Value);
        }
        else
        {
            bookings = await _bookingService.GetByUserIdAsync(userId.Value);
        }

        return Ok(bookings);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (booking, error) = await _bookingService.CreateAsync(
            userId.Value,
            request.InstructorId,
            request.ScheduledDate,
            request.StartHour
        );

        if (booking is null)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("Booking", booking.Id.ToString(), "create",
            $"user {userId.Value} with instructor {request.InstructorId}", username);

        return Created($"/api/bookings/{booking.Id}", booking);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Cancel(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, error) = await _bookingService.CancelAsync(id, userId.Value);

        if (!success)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("Booking", id.ToString(), "cancel", null, username);

        return NoContent();
    }

    [HttpGet("week")]
    public async Task<IActionResult> GetWeekBookings([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var bookings = await _bookingService.GetByDateRangeAsync(from, to);
        return Ok(bookings);
    }

    [HttpGet("available-slots")]
    public async Task<IActionResult> GetAvailableSlots([FromQuery] int instructorId, [FromQuery] DateTime date)
    {
        var slots = await _bookingService.GetAvailableSlotsAsync(instructorId, date);
        return Ok(slots);
    }

    // Admin: create booking on behalf of a user
    [HttpPost("admin")]
    public async Task<IActionResult> AdminCreate([FromBody] AdminCreateBookingRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var (booking, error) = await _bookingService.CreateAsync(
            request.UserId,
            request.InstructorId,
            request.ScheduledDate,
            request.StartHour
        );

        if (booking is null)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("Booking", booking.Id.ToString(), "admin-create",
            $"admin created booking for user {request.UserId} with instructor {request.InstructorId}", username);

        return Created($"/api/bookings/{booking.Id}", booking);
    }

    // Admin: cancel any booking without restrictions
    [HttpDelete("{id}/admin-cancel")]
    public async Task<IActionResult> AdminCancel(int id)
    {
        if (!IsAdmin()) return Forbid();

        var (success, error) = await _bookingService.AdminCancelAsync(id);

        if (!success)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("Booking", id.ToString(), "admin-cancel", null, username);

        return NoContent();
    }

    private bool IsAdmin()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value == "admin";
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }

    private string GetUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value ?? "unknown";
    }
}
