using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class TokenPacksController : ControllerBase
{
    private readonly TokenPackService _tokenPackService;
    private readonly AuditLogService _auditLogService;

    public TokenPacksController(TokenPackService tokenPackService, AuditLogService auditLogService)
    {
        _tokenPackService = tokenPackService;
        _auditLogService = auditLogService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var packs = await _tokenPackService.GetAllAsync();
        return Ok(packs);
    }

    [HttpGet("user/{userId}")]
    public async Task<IActionResult> GetByUserId(int userId)
    {
        if (!IsAdmin()) return Forbid();

        var packs = await _tokenPackService.GetByUserIdAsync(userId);
        return Ok(packs);
    }

    [HttpGet("my")]
    public async Task<IActionResult> GetMy()
    {
        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var packs = await _tokenPackService.GetByUserIdAsync(userId.Value);
        return Ok(packs);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateTokenPackRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var pack = await _tokenPackService.CreateAsync(request.UserId, request.TotalTokens, request.Description, userId.Value);

        var username = GetUsername();
        await _auditLogService.LogAsync("TokenPack", pack.Id.ToString(), "create",
            $"{pack.UserName} - {pack.TotalTokens} tokens", username);

        return Created($"/api/tokenpacks/{pack.Id}", pack);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateTokenPackRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var pack = await _tokenPackService.UpdateAsync(id, request.RemainingTokens, request.Description);
        if (pack is null) return NotFound();

        var username = GetUsername();
        await _auditLogService.LogAsync("TokenPack", id.ToString(), "update",
            $"remaining={request.RemainingTokens}, description={request.Description}", username);

        return Ok(pack);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin()) return Forbid();

        var success = await _tokenPackService.DeleteAsync(id);
        if (!success) return NotFound();

        var username = GetUsername();
        await _auditLogService.LogAsync("TokenPack", id.ToString(), "delete", null, username);

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
