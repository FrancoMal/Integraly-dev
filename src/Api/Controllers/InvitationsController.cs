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
    private readonly EmailService _emailService;
    private readonly AuditLogService _auditLogService;

    public InvitationsController(InvitationService invitationService, EmailService emailService, AuditLogService auditLogService)
    {
        _invitationService = invitationService;
        _emailService = emailService;
        _auditLogService = auditLogService;
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

        // Send invitation email
        var scheme = HttpContext.Request.Scheme;
        var host = HttpContext.Request.Host.ToString();
        var registrationLink = $"{scheme}://{host}/panel/registro?token={invitation.Token}";

        var htmlBody = $@"
<!DOCTYPE html>
<html>
<head><meta charset=""utf-8""></head>
<body style=""font-family: 'Segoe UI', Arial, sans-serif; background: #f4f6f9; margin: 0; padding: 0;"">
  <div style=""max-width: 600px; margin: 40px auto; background: #ffffff; border-radius: 12px; overflow: hidden; box-shadow: 0 2px 12px rgba(0,0,0,0.08);"">
    <div style=""background: linear-gradient(135deg, #10b981, #059669); padding: 32px; text-align: center;"">
      <h1 style=""color: #ffffff; margin: 0; font-size: 28px;"">Integraly</h1>
    </div>
    <div style=""padding: 40px 32px;"">
      <h2 style=""color: #1a1a2e; margin-top: 0;"">Has sido invitado a Integraly</h2>
      <p style=""color: #4a5568; font-size: 16px; line-height: 1.6;"">
        Se te ha invitado a unirte a la plataforma Integraly con el rol de <strong>{invitation.RoleName}</strong>.
      </p>
      <p style=""color: #4a5568; font-size: 16px; line-height: 1.6;"">
        Haz clic en el boton de abajo para crear tu cuenta:
      </p>
      <div style=""text-align: center; margin: 32px 0;"">
        <a href=""{registrationLink}"" style=""display: inline-block; background: linear-gradient(135deg, #10b981, #059669); color: #ffffff; text-decoration: none; padding: 14px 40px; border-radius: 8px; font-size: 16px; font-weight: 600;"">
          Crear mi cuenta
        </a>
      </div>
      <p style=""color: #718096; font-size: 14px;"">
        Si el boton no funciona, copia y pega este enlace en tu navegador:<br/>
        <a href=""{registrationLink}"" style=""color: #10b981; word-break: break-all;"">{registrationLink}</a>
      </p>
      <p style=""color: #a0aec0; font-size: 12px; margin-top: 32px;"">
        Esta invitacion expira en 7 dias. Si no solicitaste esta invitacion, puedes ignorar este email.
      </p>
    </div>
    <div style=""background: #f7fafc; padding: 20px 32px; text-align: center;"">
      <p style=""color: #a0aec0; font-size: 12px; margin: 0;"">— Integraly</p>
    </div>
  </div>
</body>
</html>";

        await _emailService.SendEmailAsync(request.Email, "Has sido invitado a Integraly", htmlBody);

        // Audit log
        var username = User.FindFirst(ClaimTypes.Name)?.Value ?? User.FindFirst("username")?.Value ?? "unknown";
        await _auditLogService.LogAsync("Invitation", invitation.Id.ToString(), "create",
            $"Invitacion a {request.Email} como {invitation.RoleName}", username);

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
