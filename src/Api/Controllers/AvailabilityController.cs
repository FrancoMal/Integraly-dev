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
