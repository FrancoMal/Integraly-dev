using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Api.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FichajesController : ControllerBase
{
    private readonly FichajeService _fichajeService;
    private readonly AuditLogService _auditLogService;
    private readonly AppDbContext _db;

    public FichajesController(FichajeService fichajeService, AuditLogService auditLogService, AppDbContext db)
    {
        _fichajeService = fichajeService;
        _auditLogService = auditLogService;
        _db = db;
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy([FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var entries = await _fichajeService.GetByInstructorAsync(userId.Value, from, to);
        return Ok(entries);
    }

    [HttpGet]
    public async Task<IActionResult> GetByInstructor([FromQuery] int instructorId, [FromQuery] DateTime from, [FromQuery] DateTime to)
    {
        if (!IsAdmin()) return Forbid();

        var entries = await _fichajeService.GetByInstructorAsync(instructorId, from, to);
        return Ok(entries);
    }

    [HttpGet("instructors")]
    public async Task<IActionResult> GetInstructors()
    {
        if (!IsAdmin()) return Forbid();

        var instructors = await _db.Users
            .Where(u => u.RoleId == 2 && u.IsActive)
            .OrderBy(u => u.Username)
            .Select(u => new { u.Id, Name = u.Username })
            .ToListAsync();

        return Ok(instructors);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTimeEntryRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (entry, error) = await _fichajeService.CreateAsync(userId.Value, request);

        if (entry is null)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("TimeEntry", entry.Id.ToString(), "create",
            $"Fichaje {request.Date:yyyy-MM-dd} {request.StartHour}:00-{request.EndHour}:00", username);

        return Created($"/api/fichajes/{entry.Id}", entry);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTimeEntryRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (entry, error) = await _fichajeService.UpdateAsync(id, userId.Value, request);

        if (entry is null)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("TimeEntry", id.ToString(), "update",
            $"Fichaje actualizado {entry.StartHour}:00-{entry.EndHour}:00", username);

        return Ok(entry);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var (success, error) = await _fichajeService.DeleteAsync(id, userId.Value);

        if (!success)
            return BadRequest(new { message = error });

        var username = GetUsername();
        await _auditLogService.LogAsync("TimeEntry", id.ToString(), "delete", null, username);

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
