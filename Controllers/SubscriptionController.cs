using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SubscriptionController(SubscriptionService subscriptionService) : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<SubscriptionStatusDto>> GetStatus()
    {
        try
        {
            var result = await subscriptionService.GetStatusAsync(GetUserId());
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("activate")]
    public async Task<ActionResult<SubscriptionStatusDto>> Activate([FromBody] ActivateSubscriptionDto dto)
    {
        try
        {
            var result = await subscriptionService.ActivateAsync(GetUserId(), dto);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
