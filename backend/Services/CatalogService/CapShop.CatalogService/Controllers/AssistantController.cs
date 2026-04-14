using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.DTOs.Assistant;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.CatalogService.Controllers
{
    [ApiController]
    [Route("catalog/assistant")]
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
    }
}