using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/changelog")]
public class ChangelogController : ControllerBase
{
    private readonly ChangelogService _service;
    private readonly IConfiguration _config;

    public ChangelogController(ChangelogService service, IConfiguration config)
    {
        _service = service;
        _config = config;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetListAsync(from, to, search, page, pageSize);
        return Ok(result);
    }

    [HttpGet("{date}")]
    [Authorize]
    public async Task<IActionResult> GetByDate(DateTime date)
    {
        var result = await _service.GetByDateAsync(date);
        if (result is null) return NotFound();
        return Ok(result);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateDailyChangelogRequest request)
    {
        var apiKey = _config["Changelog:ApiKey"];
        var providedKey = Request.Headers["X-Changelog-Key"].FirstOrDefault();

        if (string.IsNullOrEmpty(apiKey) || providedKey != apiKey)
            return Unauthorized("Invalid API key");

        var result = await _service.CreateOrUpdateAsync(request);
        return Ok(result);
    }

    [HttpPut("groups/{groupId}/author")]
    [Authorize]
    public async Task<IActionResult> UpdateAuthor(int groupId, [FromBody] UpdateAuthorRequest request)
    {
        var success = await _service.UpdateAuthorAsync(groupId, request.Author);
        if (!success) return NotFound();
        return Ok();
    }
}
