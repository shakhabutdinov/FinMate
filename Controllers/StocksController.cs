using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class StocksController(StockService stockService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<StockPortfolioDto>> GetPortfolio()
    {
        var userId = GetUserId();
        var result = await stockService.GetPortfolioAsync(userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<StockHoldingDto>> CreateHolding([FromBody] CreateStockHoldingDto dto)
    {
        var userId = GetUserId();
        var result = await stockService.CreateHoldingAsync(userId, dto);
        return CreatedAtAction(nameof(GetPortfolio), result);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteHolding(Guid id)
    {
        await stockService.DeleteHoldingAsync(id);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
