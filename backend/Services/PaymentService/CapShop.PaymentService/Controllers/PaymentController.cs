using CapShop.PaymentService.Application.Interfaces;
using CapShop.PaymentService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.PaymentService.Controllers;

[ApiController]
[Route("payment")]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly IPaymentAppService _paymentService;

    public PaymentController(IPaymentAppService paymentService)
    {
        _paymentService = paymentService;
    }

    private int GetUserIdStrict()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (!int.TryParse(userIdClaim, out var userId) || userId <= 0)
        {
            throw new UnauthorizedAccessException("Invalid token userId claim.");
        }

        return userId;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { service = "PaymentService", status = "Healthy" });

    [HttpPost("process")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> Process([FromBody] ProcessPaymentRequestDto request)
    {
        var userId = GetUserIdStrict();
        var response = await _paymentService.ProcessPaymentAsync(userId, request);
        return Ok(response);
    }

    [HttpPost("internal/razorpay/create-order")]
    [AllowAnonymous]
    public async Task<IActionResult> CreateRazorpayOrder([FromBody] CreateRazorpayOrderRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _paymentService.CreateRazorpayOrderAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("internal/razorpay/verify")]
    [AllowAnonymous]
    public async Task<IActionResult> VerifyRazorpayPayment([FromBody] VerifyRazorpayPaymentRequestDto request, CancellationToken cancellationToken)
    {
        var response = await _paymentService.VerifyRazorpayPaymentAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpGet("{id:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetById(int id)
    {
        var payment = await _paymentService.GetByIdAsync(id);
        if (payment is null)
        {
            return NotFound();
        }

        var userId = GetUserIdStrict();
        if (payment.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(payment);
    }

    [HttpGet("order/{orderId:int}")]
    [Authorize(Policy = "CustomerOnly")]
    public async Task<IActionResult> GetLatestByOrderId(int orderId)
    {
        var payment = await _paymentService.GetLatestByOrderIdAsync(orderId);
        if (payment is null)
        {
            return NotFound();
        }

        var userId = GetUserIdStrict();
        if (payment.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(payment);
    }

    [HttpGet("internal/order/{orderId:int}/latest")]
    [AllowAnonymous]
    public async Task<IActionResult> GetLatestByOrderIdInternal(int orderId)
    {
        var payment = await _paymentService.GetLatestByOrderIdAsync(orderId);
        if (payment is null)
        {
            return NotFound();
        }

        return Ok(payment);
    }

    [HttpPut("{id:int}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateStatus(int id, [FromBody] UpdatePaymentStatusRequestDto request)
    {
        var updated = await _paymentService.UpdateStatusAsync(id, request);
        if (updated is null)
        {
            return NotFound();
        }

        return Ok(updated);
    }
}
