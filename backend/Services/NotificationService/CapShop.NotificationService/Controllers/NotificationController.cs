using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.NotificationService.Controllers;

[ApiController]
[Route("notification")]
[Authorize]
public class NotificationController : ControllerBase
{
    private readonly INotificationAppService _notificationService;

    public NotificationController(INotificationAppService notificationService)
    {
        _notificationService = notificationService;
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
    public IActionResult Health() => Ok(new { service = "NotificationService", status = "Healthy" });

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendNotificationRequestDto request)
    {
        var userId = GetUserIdStrict();
        var item = await _notificationService.SendAsync(userId, request);
        return Ok(item);
    }

    [HttpGet("my")]
    public async Task<IActionResult> MyNotifications()
    {
        var userId = GetUserIdStrict();
        var items = await _notificationService.GetByUserAsync(userId);
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await _notificationService.GetByIdAsync(id);
        if (item is null)
        {
            return NotFound();
        }

        var userId = GetUserIdStrict();
        if (item.UserId.HasValue && item.UserId.Value != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return Ok(item);
    }

    [HttpPut("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(int id)
    {
        var userId = GetUserIdStrict();
        var item = await _notificationService.MarkAsReadAsync(id, userId);
        if (item is null)
        {
            return NotFound();
        }

        return Ok(item);
    }

    [HttpGet("admin/all")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AllNotifications()
    {
        var items = await _notificationService.GetAllAsync();
        return Ok(items);
    }
}
