using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AlpacaController(AlpacaService alpacaService) : ControllerBase
{
    // Account 

    [HttpGet("status")]
    public async Task<ActionResult<AlpacaStatusDto>> GetStatus()
        => Ok(await alpacaService.GetStatusAsync(GetUserId()));

    [HttpPost("connect")]
    public async Task<ActionResult<AlpacaAccountDataDto>> Connect([FromBody] ConnectAlpacaDto dto)
    {
        try   { return Ok(await alpacaService.ConnectAsync(GetUserId(), dto)); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpGet("account")]
    public async Task<ActionResult<AlpacaAccountDataDto>> GetAccountData()
        => Ok(await alpacaService.GetAccountDataAsync(GetUserId()));

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await alpacaService.DisconnectAsync(GetUserId());
        return NoContent();
    }

    // Market data 

    [HttpGet("bars/{symbol}")]
    public async Task<ActionResult<List<AlpacaBarDto>>> GetBars(
        string symbol,
        [FromQuery] string timeframe = "1Day",
        [FromQuery] int limit = 30)
    {
        try   { return Ok(await alpacaService.GetBarsForUserAsync(GetUserId(), symbol, timeframe, limit)); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    [HttpPost("orders")]
    public async Task<ActionResult<OrderResultDto>> PlaceOrder([FromBody] PlaceOrderDto dto)
    {
        try
        {
            var result = await alpacaService.PlaceOrderAsync(GetUserId(), dto);
            return Ok(result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = ex.Message });
        }
    }

    /// <summary>Get recent order history (last 20 by default).</summary>
    [HttpGet("orders")]
    public async Task<ActionResult<List<OrderSummaryDto>>> GetOrders(
        [FromQuery] int limit = 20)
    {
        try   { return Ok(await alpacaService.GetOrdersAsync(GetUserId(), limit)); }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    /// <summary>Cancel an open order by its order ID.</summary>
    [HttpDelete("orders/{orderId}")]
    public async Task<IActionResult> CancelOrder(string orderId)
    {
        try
        {
            await alpacaService.CancelOrderAsync(GetUserId(), orderId);
            return NoContent();
        }
        catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}