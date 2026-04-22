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

        public AssistantController(IInventoryAssistantService assistantService)
        {
            _assistantService = assistantService;
        }

        [HttpPost("query")]
        [Authorize]
        public async Task<IActionResult> Query([FromBody] AssistantQueryRequestDto request, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(request?.Message))
            {
                return BadRequest(new { message = "Message is required." });
            }

            var response = await _assistantService.QueryAsync(request, ct);
            return Ok(response);
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "AssistantService", status = "Healthy" });
    }
}
