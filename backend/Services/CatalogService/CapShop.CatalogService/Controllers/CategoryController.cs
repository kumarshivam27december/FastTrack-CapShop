using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.DTOs.Catalog;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.CatalogService.Controllers
{
    [ApiController]
    [Route("catalog/categories")]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryAppService _categoryAppService;

        public CategoryController(ICategoryAppService categoryAppService)
        {
            _categoryAppService = categoryAppService;
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAllCategories(CancellationToken ct)
        {
            var categories = await _categoryAppService.GetAllCategoriesAsync(ct);
            return Ok(categories);
        }

        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetCategoryById(int id, CancellationToken ct)
        {
            var category = await _categoryAppService.GetCategoryByIdAsync(id, ct);
            if (category is null) return NotFound();
            return Ok(category);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto, CancellationToken ct)
        {
            var category = await _categoryAppService.CreateCategoryAsync(dto, ct);
            return CreatedAtAction(nameof(GetCategoryById), new { id = category.Id }, category);
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateCategory(int id, [FromBody] UpdateCategoryDto dto, CancellationToken ct)
        {
            var ok = await _categoryAppService.UpdateCategoryAsync(id, dto, ct);
            if (!ok) return NotFound();
            return Ok(new { message = "Category updated successfully" });
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteCategory(int id, CancellationToken ct)
        {
            var ok = await _categoryAppService.DeleteCategoryAsync(id, ct);
            if (!ok) return NotFound();
            return Ok(new { message = "Category deleted successfully" });
        }
    }
}
