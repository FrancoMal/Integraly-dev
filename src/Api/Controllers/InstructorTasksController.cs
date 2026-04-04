using System.Security.Claims;
using Api.Data;
using Api.DTOs;
using Api.Models;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/instructor-tasks")]
[Authorize]
public class InstructorTasksController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly AuditLogService _auditLogService;

    public InstructorTasksController(AppDbContext db, AuditLogService auditLogService)
    {
        _db = db;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? instructorId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var query = _db.InstructorTasks
            .Include(t => t.Instructor)
            .Include(t => t.AssignedByUser)
            .AsQueryable();

        // Non-admin users can only see their own tasks
        if (!IsAdmin())
        {
            query = query.Where(t => t.InstructorId == userId.Value);
        }
        else if (instructorId.HasValue)
        {
            query = query.Where(t => t.InstructorId == instructorId.Value);
        }

        if (from.HasValue)
            query = query.Where(t => t.TaskDate >= from.Value);

        if (to.HasValue)
            query = query.Where(t => t.TaskDate <= to.Value);

        var tasks = await query
            .OrderByDescending(t => t.TaskDate)
            .ThenByDescending(t => t.CreatedAt)
            .Select(t => ToDto(t))
            .ToListAsync();

        return Ok(tasks);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var task = await _db.InstructorTasks
            .Include(t => t.Instructor)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
            return NotFound(new { message = "Tarea no encontrada" });

        // Non-admin can only see own tasks
        if (!IsAdmin() && task.InstructorId != userId.Value)
            return Forbid();

        return Ok(ToDto(task));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInstructorTaskRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        // Non-admin can only create tasks for themselves
        if (!IsAdmin() && request.InstructorId != userId.Value)
            return Forbid();

        var task = new InstructorTask
        {
            InstructorId = request.InstructorId,
            Title = request.Title,
            Description = request.Description,
            TaskType = request.TaskType,
            TaskDate = request.TaskDate,
            StartHour = request.StartHour,
            EndHour = request.EndHour,
            HoursWorked = request.HoursWorked,
            Status = string.IsNullOrEmpty(request.Status) ? "asignada" : request.Status,
            AssignedByUserId = IsAdmin() ? userId.Value : null,
            CreatedAt = DateTime.UtcNow
        };

        _db.InstructorTasks.Add(task);
        await _db.SaveChangesAsync();

        // Reload with navigation properties
        await _db.Entry(task).Reference(t => t.Instructor).LoadAsync();
        if (task.AssignedByUserId.HasValue)
            await _db.Entry(task).Reference(t => t.AssignedByUser).LoadAsync();

        var username = GetUsername();
        await _auditLogService.LogAsync("InstructorTask", task.Id.ToString(), "create",
            $"instructor {request.InstructorId}: {request.Title}", username);

        return Created($"/api/instructor-tasks/{task.Id}", ToDto(task));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateInstructorTaskRequest request)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var task = await _db.InstructorTasks
            .Include(t => t.Instructor)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
            return NotFound(new { message = "Tarea no encontrada" });

        // Non-admin can only update own tasks
        if (!IsAdmin() && task.InstructorId != userId.Value)
            return Forbid();

        if (request.Title is not null) task.Title = request.Title;
        if (request.Description is not null) task.Description = request.Description;
        if (request.TaskType is not null) task.TaskType = request.TaskType;
        if (request.TaskDate.HasValue) task.TaskDate = request.TaskDate.Value;
        if (request.StartHour.HasValue) task.StartHour = request.StartHour.Value;
        if (request.EndHour.HasValue) task.EndHour = request.EndHour.Value;
        if (request.HoursWorked.HasValue) task.HoursWorked = request.HoursWorked.Value;
        if (request.Status is not null) task.Status = request.Status;

        await _db.SaveChangesAsync();

        var username = GetUsername();
        await _auditLogService.LogAsync("InstructorTask", id.ToString(), "update", null, username);

        return Ok(ToDto(task));
    }

    [HttpPut("{id}/complete")]
    public async Task<IActionResult> Complete(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var task = await _db.InstructorTasks
            .Include(t => t.Instructor)
            .Include(t => t.AssignedByUser)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (task is null)
            return NotFound(new { message = "Tarea no encontrada" });

        // Non-admin can only complete own tasks
        if (!IsAdmin() && task.InstructorId != userId.Value)
            return Forbid();

        task.Status = "completada";
        task.CompletedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        var username = GetUsername();
        await _auditLogService.LogAsync("InstructorTask", id.ToString(), "complete", null, username);

        return Ok(ToDto(task));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var task = await _db.InstructorTasks.FindAsync(id);

        if (task is null)
            return NotFound(new { message = "Tarea no encontrada" });

        // Non-admin can only delete own tasks
        if (!IsAdmin() && task.InstructorId != userId.Value)
            return Forbid();

        _db.InstructorTasks.Remove(task);
        await _db.SaveChangesAsync();

        var username = GetUsername();
        await _auditLogService.LogAsync("InstructorTask", id.ToString(), "delete", null, username);

        return NoContent();
    }

    private static InstructorTaskDto ToDto(InstructorTask t) => new(
        t.Id,
        t.InstructorId,
        t.Instructor != null ? $"{t.Instructor.FirstName} {t.Instructor.LastName}".Trim() : "",
        t.Title,
        t.Description,
        t.TaskType,
        t.TaskDate,
        t.StartHour,
        t.EndHour,
        t.HoursWorked,
        t.Status,
        t.AssignedByUserId,
        t.AssignedByUser != null ? $"{t.AssignedByUser.FirstName} {t.AssignedByUser.LastName}".Trim() : null,
        t.CreatedAt,
        t.CompletedAt
    );

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
