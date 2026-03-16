using System.Security.Claims;
using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class InvitationsController : ControllerBase
{
    private readonly InvitationService _invitationService;

    public InvitationsController(InvitationService invitationService)
    {
        _invitationService = invitationService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        if (!IsAdmin()) return Forbid();

        var invitations = await _invitationService.GetAllAsync();
        return Ok(invitations);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateInvitationRequest request)
    {
        if (!IsAdmin()) return Forbid();

        var userId = GetUserId();
        if (userId is null) return Unauthorized();

        var invitation = await _invitationService.CreateAsync(request.Email, request.RoleId, userId.Value);
        return Created($"/api/invitations/{invitation.Id}", invitation);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        if (!IsAdmin()) return Forbid();

        var result = await _invitationService.DeleteAsync(id);
        if (!result) return NotFound(new { message = "Invitacion no encontrada o ya fue usada" });
        return NoContent();
    }

    [HttpGet("token/{token}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetByToken(string token)
    {
        var invitation = await _invitationService.GetByTokenAsync(token);
        if (invitation is null) return NotFound(new { message = "Invitacion no encontrada" });
        return Ok(invitation);
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
}
