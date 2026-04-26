using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PfmController(PfmService pfmService) : ControllerBase
{
    [HttpGet("overview")]
    public async Task<ActionResult<PfmOverviewDto>> GetOverview()
    {
        var userId = GetUserId();
        var result = await pfmService.GetOverviewAsync(userId);
        return Ok(result);
    }

    [HttpGet("transactions")]
    public async Task<ActionResult<List<TransactionDto>>> GetTransactions([FromQuery] int? limit)
    {
        var userId = GetUserId();
        var result = await pfmService.GetTransactionsAsync(userId, limit);
        return Ok(result);
    }

    [HttpPost("transactions")]
    public async Task<ActionResult<TransactionDto>> CreateTransaction([FromBody] CreateTransactionDto dto)
    {
        var userId = GetUserId();
        var result = await pfmService.CreateTransactionAsync(userId, dto);
        return CreatedAtAction(nameof(GetTransactions), result);
    }

    [HttpGet("goals")]
    public async Task<ActionResult<List<GoalDto>>> GetGoals()
    {
        var userId = GetUserId();
        var result = await pfmService.GetGoalsAsync(userId);
        return Ok(result);
    }

    [HttpPost("goals")]
    public async Task<ActionResult<GoalDto>> CreateGoal([FromBody] CreateGoalDto dto)
    {
        var userId = GetUserId();
        var result = await pfmService.CreateGoalAsync(userId, dto);
        return CreatedAtAction(nameof(GetGoals), result);
    }

    [HttpPost("goals/{id}/contribute")]
    public async Task<ActionResult<GoalDto>> ContributeToGoal(Guid id, [FromBody] ContributeGoalDto dto)
    {
        var userId = GetUserId();
        var result = await pfmService.ContributeToGoalAsync(userId, id, dto.Amount);
        if (result is null) return NotFound();
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
