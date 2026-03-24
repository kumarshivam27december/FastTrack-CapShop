using CapShop.AdminService.Application.Interfaces;
using CapShop.AdminService.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.AdminService.Controllers;

[ApiController]
[Route("admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IAdminAppService _adminService;

    public AdminController(IAdminAppService adminService)
    {
        _adminService = adminService;
    }

    [HttpGet("health")]
    [AllowAnonymous]
    public IActionResult Health() => Ok(new { service = "AdminService", status = "Healthy" });

    [HttpGet("dashboard/summary")]
    public async Task<IActionResult> GetDashboardSummary(CancellationToken ct)
    {
        var token = GetBearerTokenOrThrow();
        var result = await _adminService.GetDashboardSummaryAsync(token, ct);
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders(CancellationToken ct)
    {
        var token = GetBearerTokenOrThrow();
        var result = await _adminService.GetOrdersAsync(token, ct);
        return Ok(result);
    }

    [HttpPut("orders/{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusAdminRequest request, CancellationToken ct)
    {
        var token = GetBearerTokenOrThrow();
        var adminEmail = User.Identity?.Name ?? "admin@unknown";
        var ok = await _adminService.UpdateOrderStatusAsync(id, request, token, adminEmail, ct);
        if (!ok) return BadRequest(new { message = "Order status update failed or invalid transition." });
        return Ok(new { message = "Order status updated." });
    }

    [HttpGet("reports/status-split")]
    public async Task<IActionResult> GetStatusSplit(CancellationToken ct)
    {
        var token = GetBearerTokenOrThrow();
        var result = await _adminService.GetStatusSplitAsync(token, ct);
        return Ok(result);
    }

    [HttpGet("reports/sales")]
    public async Task<IActionResult> GetSalesReport([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (from > to) return BadRequest(new { message = "from must be <= to" });
        var token = GetBearerTokenOrThrow();
        var result = await _adminService.GetSalesReportAsync(from, to, token, ct);
        return Ok(result);
    }

    [HttpGet("reports/export/csv")]
    public async Task<IActionResult> ExportSalesCsv([FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        if (from > to) return BadRequest(new { message = "from must be <= to" });
        var token = GetBearerTokenOrThrow();
        var csv = await _adminService.BuildSalesCsvAsync(from, to, token, ct);
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", $"sales-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
    }

    private string GetBearerTokenOrThrow()
    {
        var raw = Request.Headers.Authorization.ToString();
        if (string.IsNullOrWhiteSpace(raw))
        {
            throw new UnauthorizedAccessException("Authorization header missing.");
        }

        return raw;
    }
}