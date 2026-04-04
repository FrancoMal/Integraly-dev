using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BackupController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromServices] BackupService backupService)
    {
        var backups = await backupService.GetAllAsync();
        return Ok(backups);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromServices] BackupService backupService)
    {
        var result = await backupService.CreateBackupAsync("manual");
        if (result == null)
            return StatusCode(500, new { error = "Error al crear backup" });
        return Ok(result);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id, [FromServices] BackupService backupService)
    {
        var result = await backupService.DeleteBackupAsync(id);
        if (!result) return NotFound();
        return Ok(new { message = "Backup eliminado" });
    }

    [HttpGet("{id}/download")]
    public IActionResult Download(int id, [FromServices] BackupService backupService)
    {
        var filePath = backupService.GetBackupFilePath(id);
        if (filePath == null) return NotFound(new { error = "Archivo no encontrado" });

        var fileName = Path.GetFileName(filePath);
        return PhysicalFile(filePath, "application/octet-stream", fileName);
    }

    [HttpGet("schedule")]
    public async Task<IActionResult> GetSchedule([FromServices] BackupService backupService)
    {
        var schedule = await backupService.GetScheduleAsync();
        return Ok(schedule);
    }

    [HttpPut("schedule")]
    public async Task<IActionResult> UpdateSchedule([FromBody] BackupScheduleDto dto, [FromServices] BackupService backupService)
    {
        if (dto.IntervalHours < 1) dto.IntervalHours = 1;
        if (dto.RetentionDays < 1) dto.RetentionDays = 1;
        await backupService.UpdateScheduleAsync(dto);
        return Ok(new { message = "Programacion actualizada" });
    }
}
