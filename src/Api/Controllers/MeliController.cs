using Api.DTOs;
using Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MeliController : ControllerBase
{
    private readonly MeliAccountService _accountService;
    private readonly MeliItemService _itemService;
    private readonly MeliOrderService _orderService;

    public MeliController(MeliAccountService accountService, MeliItemService itemService, MeliOrderService orderService)
    {
        _accountService = accountService;
        _itemService = itemService;
        _orderService = orderService;
    }

    // --- Accounts ---
    [HttpGet("accounts")]
    public async Task<IActionResult> GetAccounts()
    {
        var accounts = await _accountService.GetAccountsAsync();
        return Ok(accounts);
    }

    [HttpGet("auth-url")]
    public async Task<IActionResult> GetAuthUrl()
    {
        var url = await _accountService.GetAuthUrlAsync();
        if (url is null) return BadRequest(new { message = "MercadoLibre no esta configurado" });
        return Ok(new { url });
    }

    [HttpPost("callback")]
    public async Task<IActionResult> Callback([FromBody] MeliCallbackRequest request)
    {
        var (account, error) = await _accountService.HandleCallbackAsync(request.Code);
        if (error is not null) return BadRequest(new { message = error });
        return Ok(account);
    }

    [HttpDelete("accounts/{id}")]
    public async Task<IActionResult> DeleteAccount(int id)
    {
        var deleted = await _accountService.DeleteAccountAsync(id);
        if (!deleted) return NotFound(new { message = "Cuenta no encontrada" });
        return Ok(new { message = "Cuenta desconectada" });
    }

    // --- Orders ---
    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] DateTime? from, [FromQuery] DateTime? to, [FromQuery] int? accountId)
    {
        var orders = await _orderService.GetOrdersAsync(from, to, accountId);
        return Ok(orders);
    }

    [HttpPost("orders/sync")]
    public async Task<IActionResult> SyncOrders()
    {
        try
        {
            var count = await _orderService.SyncOrdersAsync(DateTime.UtcNow.AddDays(-30), DateTime.UtcNow);
            return Ok(new { message = $"Se sincronizaron {count} ordenes", count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al sincronizar ordenes", error = ex.Message });
        }
    }

    // --- Items ---
    [HttpGet("items")]
    public async Task<IActionResult> GetItems([FromQuery] int? accountId, [FromQuery] string? status)
    {
        var items = await _itemService.GetItemsAsync(accountId, status);
        return Ok(items);
    }

    [HttpPost("items/sync")]
    public async Task<IActionResult> SyncItems()
    {
        try
        {
            var count = await _itemService.SyncItemsAsync();
            return Ok(new { message = $"Se sincronizaron {count} items", count });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Error al sincronizar items", error = ex.Message });
        }
    }

    [HttpPut("items/{meliItemId}")]
    public async Task<IActionResult> UpdateItem(string meliItemId, [FromBody] UpdateMeliItemRequest request)
    {
        var (success, error) = await _itemService.UpdateItemAsync(meliItemId, request);
        if (!success) return BadRequest(new { message = error });
        return Ok(new { message = "Item actualizado" });
    }

    [HttpGet("items/{meliItemId}/details")]
    public async Task<IActionResult> GetItemDetails(string meliItemId)
    {
        var details = await _itemService.GetItemDetailsAsync(meliItemId);
        if (details is null) return NotFound(new { message = "Item no encontrado" });
        return Ok(details);
    }
}
