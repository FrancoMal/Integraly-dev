using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AvailabilityController : ControllerBase
{
    private readonly AvailabilityService _availabilityService;

    public AvailabilityController(AvailabilityService availabilityService)
    {
        _availabilityService = availabilityService;
    }

    [HttpGet("instructor/{instructorId}")]
    public async Task<IActionResult> GetByInstructorId(int instructorId)
    {
        var availability = await _availabilityService.GetByInstructorIdAsync(instructorId);
        return Ok(availability);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (!IsInstructor()) return Forbid();

        var availability = await _availabilityService.GetByInstructorIdAsync(userId.Value);
        return Ok(availability);
    }

    [HttpPut]
    public async Task<IActionResult> SetAvailability([FromBody] List<SetAvailabilityRequest> requests)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        if (!IsInstructor()) return Forbid();

        var results = await _availabilityService.BulkSetAsync(userId.Value, requests);
        return Ok(results);
    }

    // Get week overrides for an instructor
    [HttpGet("week/{instructorId}")]
    public async Task<IActionResult> GetWeekAvailability(int instructorId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var availability = await _availabilityService.GetWeekAvailabilityAsync(instructorId, from, to);
        return Ok(availability);
    }

    // Get my week overrides
    [HttpGet("my-week")]
    public async Task<IActionResult> GetMyWeekAvailability([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (!IsInstructor()) return Forbid();
        var availability = await _availabilityService.GetWeekAvailabilityAsync(userId.Value, from, to);
        return Ok(availability);
    }

    // Set week-specific overrides
    [HttpPut("week")]
    public async Task<IActionResult> SetWeekAvailability([FromBody] SetWeekAvailabilityRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();
        if (!IsInstructor()) return Forbid();
        var results = await _availabilityService.SetWeekAvailabilityAsync(userId.Value, request.WeekStart, request.Slots);
        return Ok(results);
    }

    // Admin: toggle availability for a specific instructor at a specific date+hour
    [HttpPut("admin-toggle")]
    public async Task<IActionResult> AdminToggleAvailability([FromBody] AdminToggleAvailabilityRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var result = await _availabilityService.ToggleSingleSlotAsync(
            request.InstructorId, request.Date, request.StartHour, request.IsActive);
        return Ok(result);
    }

    // Admin: bulk toggle availability for multiple date+hour slots
    [HttpPut("admin-bulk-toggle")]
    public async Task<IActionResult> AdminBulkToggleAvailability([FromBody] AdminBulkToggleRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var count = await _availabilityService.BulkToggleSlotsAsync(
            request.InstructorId, request.Slots, request.IsActive);
        return Ok(new { updated = count });
    }

    // Admin: copy availability from previous week to target week
    [HttpPost("copy-previous-week")]
    public async Task<IActionResult> CopyPreviousWeek([FromBody] CopyWeekRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var count = await _availabilityService.CopyWeekAvailabilityAsync(request.TargetWeekStart);
        return Ok(new { copied = count });
    }

    private bool IsAdmin()
    {
        return User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value == "admin";
    }

    private bool IsInstructor()
    {
        var role = User.FindFirst(ClaimTypes.Role)?.Value;
        return role == "instructor" || role == "admin";
    }

    private int? GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return claim is not null ? int.Parse(claim) : null;
    }
}
