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
public class BookingsController : ControllerBase
{
    private readonly BookingService _bookingService;
    private readonly AuditLogService _auditLogService;
    private readonly AppDbContext _db;

    public BookingsController(BookingService bookingService, AuditLogService auditLogService, AppDbContext db)
    {
        _bookingService = bookingService;
        _auditLogService = auditLogService;
        _db = db;
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

        // Strip AdminNotes for non-admin users
        if (!IsAdmin())
        {
            bookings = bookings.Select(b => b with { AdminNotes = null }).ToList();
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
            request.StartHour,
            request.UserNotes
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
            request.StartHour,
            request.UserNotes
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

    // Admin: mark booking as completed
    [HttpPut("{id}/complete")]
    public async Task<IActionResult> AdminComplete(int id)
    {
        if (!IsAdmin()) return Forbid();

        var (success, error) = await _bookingService.AdminCompleteAsync(id);

        if (!success)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("Booking", id.ToString(), "complete", null, username);

        return NoContent();
    }

    // Update notes on a booking
    [HttpPut("{id}/notes")]
    public async Task<IActionResult> UpdateNotes(int id, [FromBody] UpdateBookingNotesRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var booking = await _db.Bookings.Include(b => b.User).Include(b => b.Instructor).FirstOrDefaultAsync(b => b.Id == id);
        if (booking is null) return NotFound();

        // Users can only update their own UserNotes
        if (!IsAdmin() && booking.UserId != userId.Value) return Forbid();

        if (request.UserNotes is not null)
            booking.UserNotes = request.UserNotes;

        // Only admin can update AdminNotes
        if (IsAdmin() && request.AdminNotes is not null)
            booking.AdminNotes = request.AdminNotes;

        await _db.SaveChangesAsync();

        var dto = new BookingDto(
            booking.Id,
            booking.UserId,
            booking.User?.Username ?? "",
            booking.InstructorId,
            booking.Instructor?.Username ?? "",
            booking.ScheduledDate,
            booking.StartHour,
            booking.Status,
            booking.MeetLink,
            booking.UserNotes,
            IsAdmin() ? booking.AdminNotes : null,
            booking.CreatedAt
        );

        return Ok(dto);
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
