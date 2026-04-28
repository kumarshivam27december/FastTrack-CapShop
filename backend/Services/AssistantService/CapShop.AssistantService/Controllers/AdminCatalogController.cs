using CapShop.AssistantService.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("admin/catalog")]
[Authorize(Roles = "Admin")]
public class AdminCatalogController : ControllerBase
{
    private readonly IAssistantKnowledgeService _knowledge;

    public AdminCatalogController(IAssistantKnowledgeService knowledge) => _knowledge = knowledge;

    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
        if (file == null) return BadRequest("file is required");
        using var s = file.OpenReadStream();
        var count = await _knowledge.LoadFromStreamAsync(s, file.FileName);
        return Ok(new { imported = count });
    }
}
