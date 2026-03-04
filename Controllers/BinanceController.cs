using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class BinanceController(BinanceService binanceService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<BinanceStatusDto>> GetStatus()
    {
        var result = await binanceService.GetStatusAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("connect")]
    public async Task<ActionResult<BinanceAccountDataDto>> Connect([FromBody] ConnectBinanceDto dto)
    {
        try
        {
            var result = await binanceService.ConnectAsync(GetUserId(), dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("account")]
    public async Task<ActionResult<BinanceAccountDataDto>> GetAccountData()
    {
        var result = await binanceService.GetAccountDataAsync(GetUserId());
        return Ok(result);
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await binanceService.DisconnectAsync(GetUserId());
        return NoContent();
    }

    [HttpGet("klines/{symbol}")]
    public async Task<ActionResult<List<KlinePointDto>>> GetKlines(string symbol, [FromQuery] string interval = "1d", [FromQuery] int limit = 30)
    {
        try
        {
            var result = await BinanceService.GetKlinesAsync(symbol, interval, limit);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
