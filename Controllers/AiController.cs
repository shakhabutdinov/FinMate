using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AiController(AiService aiService) : ControllerBase
{
    [HttpGet("history")]
    public async Task<ActionResult<List<ChatMessageDto>>> GetHistory()
    {
        var userId = GetUserId();
        var result = await aiService.GetChatHistoryAsync(userId);
        return Ok(result);
    }

    [HttpPost("message")]
    public async Task<ActionResult<ChatMessageDto>> SendMessage([FromBody] SendMessageDto dto)
    {
        var userId = GetUserId();
        var result = await aiService.SendMessageAsync(userId, dto);
        return Ok(result);
    }

    [HttpGet("quick-questions")]
    public ActionResult<string[]> GetQuickQuestions()
    {
        return Ok(AiService.GetQuickQuestions());
    }

    [HttpDelete("history")]
    public async Task<IActionResult> ClearHistory()
    {
        var userId = GetUserId();
        await aiService.ClearHistoryAsync(userId);
        return NoContent();
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
