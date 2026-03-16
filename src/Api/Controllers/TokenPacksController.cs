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

    public TokenPacksController(TokenPackService tokenPackService)
    {
        _tokenPackService = tokenPackService;
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
        return Created($"/api/tokenpacks/{pack.Id}", pack);
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
