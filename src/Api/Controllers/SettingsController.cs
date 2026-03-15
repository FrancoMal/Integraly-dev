using Api.Data;
using Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var settings = await _db.AppSettings.ToListAsync();
        return Ok(settings.ToDictionary(s => s.Key, s => s.Value));
    }

    [HttpGet("{key}")]
    public async Task<IActionResult> Get(string key)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        if (setting is null) return NotFound();
        return Ok(new { setting.Key, setting.Value });
    }

    [HttpPut("{key}")]
    public async Task<IActionResult> Update(string key, [FromBody] SettingUpdateDto dto)
    {
        var setting = await _db.AppSettings.FindAsync(key);
        if (setting is null)
        {
            setting = new AppSetting { Key = key, Value = dto.Value };
            _db.AppSettings.Add(setting);
        }
        else
        {
            setting.Value = dto.Value;
            setting.UpdatedAt = DateTime.UtcNow;
        }
        await _db.SaveChangesAsync();
        return Ok(new { setting.Key, setting.Value });
    }
}

public class SettingUpdateDto
{
    public string Value { get; set; } = string.Empty;
}
