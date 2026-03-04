using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using aspnet.DTOs;
using aspnet.Services;

namespace aspnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PaymentController(PaymentService paymentService) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<PaymentResponseDto>> CreatePayment([FromBody] CreatePaymentDto dto)
    {
        var userId = GetUserId();
        var result = await paymentService.CreatePaymentAsync(userId, dto);
        return Ok(result);
    }

    [HttpGet("config")]
    public ActionResult<PaymentConfigDto> GetConfig()
    {
        var result = paymentService.GetPaymentConfig();
        return Ok(result);
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
