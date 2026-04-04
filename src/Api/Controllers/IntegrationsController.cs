using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class IntegrationsController : ControllerBase
{
    private readonly IntegrationService _service;
    private readonly AiService _aiService;

    public IntegrationsController(IntegrationService service, AiService aiService)
    {
        _service = service;
        _aiService = aiService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var items = await _service.GetAllAsync();
        return Ok(items);
    }

    [HttpGet("{provider}")]
    public async Task<IActionResult> GetByProvider(string provider)
    {
        var item = await _service.GetByProviderAsync(provider);
        if (item is null) return NotFound(new { message = "Integracion no encontrada" });
        return Ok(item);
    }

    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SaveIntegrationRequest request)
    {
        try
        {
            var result = await _service.SaveAsync(request);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al guardar integracion", error = ex.Message });
        }
    }

    [HttpDelete("{provider}")]
    public async Task<IActionResult> Delete(string provider)
    {
        var deleted = await _service.DeleteAsync(provider);
        if (!deleted) return NotFound(new { message = "Integracion no encontrada" });
        return Ok(new { message = "Integracion eliminada" });
    }

    [HttpGet("openai/models")]
    public async Task<IActionResult> GetOpenAiModels()
    {
        try
        {
            var models = await _aiService.GetOpenAiModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener modelos", error = ex.Message });
        }
    }

    [HttpGet("claude/models")]
    public async Task<IActionResult> GetClaudeModels()
    {
        try
        {
            var models = await _aiService.GetClaudeModelsAsync();
            return Ok(models);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al obtener modelos", error = ex.Message });
        }
    }

    [HttpPost("email-smtp/test")]
    public async Task<IActionResult> TestEmail()
    {
        try
        {
            var integration = await _service.GetRawByProviderAsync("email-smtp");
            if (integration is null) return BadRequest(new { message = "Email no configurado" });

            var settings = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(integration.Settings ?? "{}");
            var smtpHost = settings?.GetValueOrDefault("smtpHost")?.ToString() ?? "";
            var smtpPort = int.TryParse(settings?.GetValueOrDefault("smtpPort")?.ToString(), out var p) ? p : 587;
            var smtpTls = settings?.GetValueOrDefault("smtpTls")?.ToString() == "True" || settings?.GetValueOrDefault("smtpTls")?.ToString() == "true";
            var fromAddress = settings?.GetValueOrDefault("fromAddress")?.ToString() ?? integration.AppId ?? "";
            var fromName = settings?.GetValueOrDefault("fromName")?.ToString() ?? "Integraly";
            var username = settings?.GetValueOrDefault("username")?.ToString() ?? integration.AppId ?? "";

            using var client = new System.Net.Mail.SmtpClient(smtpHost, smtpPort);
            client.Credentials = new System.Net.NetworkCredential(username, integration.AppSecret);
            client.EnableSsl = smtpTls;

            var message = new System.Net.Mail.MailMessage();
            message.From = new System.Net.Mail.MailAddress(fromAddress, fromName);
            message.To.Add(fromAddress);
            message.Subject = "Email de prueba - Integraly";
            message.Body = "<h2>Email de prueba</h2><p>Si ves este email, la configuracion SMTP esta funcionando correctamente.</p>";
            message.IsBodyHtml = true;

            await client.SendMailAsync(message);
            return Ok(new { message = "Email de prueba enviado correctamente" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al enviar email de prueba", error = ex.Message });
        }
    }
}
