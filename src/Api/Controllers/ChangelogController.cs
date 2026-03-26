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

    public ChangelogController(ChangelogService service)
    {
        _service = service;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetList(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] string? search,
        [FromQuery] string? tag,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetListAsync(from, to, search, tag, page, pageSize);
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
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateDailyChangelogRequest request)
    {
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
