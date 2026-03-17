using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly UserService _userService;
    private readonly AuditLogService _auditLogService;

    public UsersController(UserService userService, AuditLogService auditLogService)
    {
        _userService = userService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    [HttpGet("instructors")]
    public async Task<IActionResult> GetInstructors()
    {
        var users = await _userService.GetAllAsync();
        var instructors = users.Where(u => u.Role == "instructor" && u.IsActive).ToList();
        return Ok(instructors);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _userService.GetByIdAsync(id);
        if (user is null) return NotFound(new { message = "Usuario no encontrado" });
        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _userService.CreateAsync(request);
        if (user is null) return Conflict(new { message = "El usuario o email ya existe, o el rol es invalido" });

        var username = GetUsername();
        await _auditLogService.LogAsync("User", user.Id.ToString(), "create",
            $"{user.Username} ({user.Role})", username);

        return Created($"/api/users/{user.Id}", user);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var user = await _userService.UpdateAsync(id, request);
        if (user is null) return NotFound(new { message = "Usuario no encontrado o datos invalidos" });

        var username = GetUsername();
        await _auditLogService.LogAsync("User", id.ToString(), "update",
            $"{user.Username}", username);

        return Ok(user);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin()) return Forbid();

        var result = await _userService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Usuario no encontrado" });

        var username = GetUsername();
        await _auditLogService.LogAsync("User", id.ToString(), "delete", null, username);

        return NoContent();
    }

    private bool IsAdmin()
    {
        return User.FindFirst(ClaimTypes.Role)?.Value == "admin";
    }

    private string GetUsername()
    {
        return User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value ?? "unknown";
    }
}
