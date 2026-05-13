using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CryptoController(CryptoService cryptoService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CryptoPortfolioDto>> GetPortfolio()
    {
        var result = await cryptoService.GetPortfolioAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("price/{symbol}")]
    public async Task<ActionResult> GetLivePrice(string symbol)
    {
        var price = await cryptoService.GetLivePriceAsync(symbol.ToUpper());

        if (price <= 0)
            return NotFound(new { error = $"Could not fetch live price for {symbol.ToUpper()}. Check the symbol and try again." });

        return Ok(new
        {
            symbol   = symbol.ToUpper(),
            price,
            currency = "USDT"
        });
    }

    [HttpPost]
    public async Task<ActionResult<CryptoHoldingDto>> CreateHolding([FromBody] CreateCryptoHoldingDto dto)
    {
        var result = await cryptoService.CreateHoldingAsync(GetUserId(), dto);
        return CreatedAtAction(nameof(GetPortfolio), result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHolding(Guid id)
    {
        await cryptoService.DeleteHoldingAsync(id);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}