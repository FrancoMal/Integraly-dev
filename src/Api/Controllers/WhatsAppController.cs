using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class WhatsAppController : ControllerBase
{
    private readonly WhatsAppService _service;

    public WhatsAppController(WhatsAppService service)
    {
        _service = service;
    }

    [HttpGet("status")]
    public async Task<IActionResult> GetStatus()
    {
        var status = await _service.GetStatusAsync();
        return Ok(status);
    }

    [HttpPost("link")]
    public async Task<IActionResult> StartLinking()
    {
        try
        {
            await _service.StartLinkingAsync();
            return Ok(new { message = "Vinculacion iniciada" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al iniciar vinculacion", error = ex.Message });
        }
    }

    [HttpGet("qr")]
    [AllowAnonymous]
    public async Task<IActionResult> GetQr()
    {
        try
        {
            var qr = await _service.GetQrScreenshotAsync();
            return File(qr, "image/png");
        }
        catch
        {
            return NotFound(new { message = "QR no disponible" });
        }
    }

    [HttpGet("check-linked")]
    public async Task<IActionResult> CheckLinked()
    {
        var linked = await _service.CheckLinkedAsync();
        return Ok(new { linked });
    }

    [HttpPost("unlink")]
    public async Task<IActionResult> Unlink()
    {
        await _service.UnlinkAsync();
        return Ok(new { message = "WhatsApp desvinculado" });
    }

    [HttpPost("cancel-link")]
    public async Task<IActionResult> CancelLink()
    {
        await _service.CancelLinkingAsync();
        return Ok(new { message = "Vinculacion cancelada" });
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendWhatsAppRequest request)
    {
        try
        {
            var result = await _service.SendMessageAsync(request.Phone, request.Message);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al enviar mensaje", error = ex.Message });
        }
    }

    [HttpPost("send-bulk")]
    public async Task<IActionResult> SendBulk([FromBody] SendBulkWhatsAppRequest request)
    {
        try
        {
            var results = await _service.SendBulkAsync(request.Recipients, request.Message);
            return Ok(results);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al enviar mensajes", error = ex.Message });
        }
    }
}
