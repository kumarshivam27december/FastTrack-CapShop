using System.Net.Http.Json;
using CapShop.AssistantService.Application.Interfaces;
using CapShop.AssistantService.DTOs.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.AssistantService.Controllers
{
    [ApiController]
    [Route("assistant")]
    public class AssistantController : ControllerBase
    {
        private readonly IInventoryAssistantService _assistantService;
        private readonly IHttpClientFactory _httpFactory;

        public AssistantController(IInventoryAssistantService assistantService, IHttpClientFactory httpFactory)
        {
            _assistantService = assistantService;
            _httpFactory = httpFactory;
        }

        [HttpPost("query")]
        [Authorize]
        public async Task<IActionResult> Query([FromBody] AssistantQueryRequestDto request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            // Extract user ID from JWT claims
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            request.UserId = userId ?? string.Empty;

            // Get Authorization header for downstream service calls
            var authHeader = HttpContext.Request.Headers["Authorization"].ToString();

            var response = await _assistantService.QueryAsync(request, authHeader, ct);
            return Ok(response);
        }

        [HttpGet("cart")]
        [Authorize]
        public async Task<IActionResult> Cart(CancellationToken ct)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var client = _httpFactory.CreateClient("order");
            using var resp = await client.GetAsync($"api/cart/{Uri.EscapeDataString(userId)}", ct);
            if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode);
            var payload = await resp.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
            return Ok(payload);
        }

        [HttpGet("orders")]
        [Authorize]
        public async Task<IActionResult> Orders(CancellationToken ct)
        {
            var userId = User.FindFirst("sub")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrWhiteSpace(userId)) return Unauthorized();

            var client = _httpFactory.CreateClient("order");
            using var resp = await client.GetAsync($"api/orders/user/{Uri.EscapeDataString(userId)}", ct);
            if (!resp.IsSuccessStatusCode) return StatusCode((int)resp.StatusCode);
            var payload = await resp.Content.ReadFromJsonAsync<object>(cancellationToken: ct);
            return Ok(payload);
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "AssistantService", status = "Healthy" });
    }
}
