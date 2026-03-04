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
        var userId = GetUserId();
        var result = await cryptoService.GetPortfolioAsync(userId);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<CryptoHoldingDto>> CreateHolding([FromBody] CreateCryptoHoldingDto dto)
    {
        var userId = GetUserId();
        var result = await cryptoService.CreateHoldingAsync(userId, dto);
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
