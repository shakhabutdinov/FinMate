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
    [HttpGet("status")]
    public async Task<ActionResult<AlpacaStatusDto>> GetStatus()
    {
        var result = await alpacaService.GetStatusAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("connect")]
    public async Task<ActionResult<AlpacaAccountDataDto>> Connect([FromBody] ConnectAlpacaDto dto)
    {
        try
        {
            var result = await alpacaService.ConnectAsync(GetUserId(), dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("account")]
    public async Task<ActionResult<AlpacaAccountDataDto>> GetAccountData()
    {
        var result = await alpacaService.GetAccountDataAsync(GetUserId());
        return Ok(result);
    }

    [HttpDelete("disconnect")]
    public async Task<IActionResult> Disconnect()
    {
        await alpacaService.DisconnectAsync(GetUserId());
        return NoContent();
    }

    [HttpGet("bars/{symbol}")]
    public async Task<ActionResult<List<AlpacaBarDto>>> GetBars(
        string symbol,
        [FromQuery] string timeframe = "1Day",
        [FromQuery] int limit = 30)
    {
        try
        {
            var result = await alpacaService.GetBarsForUserAsync(GetUserId(), symbol, timeframe, limit);
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
