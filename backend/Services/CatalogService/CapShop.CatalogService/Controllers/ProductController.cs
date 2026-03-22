using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.CatalogService.Controllers
{

    [ApiController]
    [Route("catalog")]
    public class ProductController : ControllerBase
    {
        private readonly IProductService _productService;
        public ProductController(IProductService productService)
        {
            _productService = productService;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "CatalogService", status = "Healthy" });

        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeatured()
        {
            var featured = await _productService.GetFeaturedProductsAsync();
            return Ok(featured);
        }

        [HttpGet("products")]
        [AllowAnonymous]
        public async Task<IActionResult> SearchProducts(
        [FromQuery] string? query,
        [FromQuery] int? categoryId,
        [FromQuery] decimal? minPrice,
        [FromQuery] decimal? maxPrice,
        [FromQuery] string? sortBy,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10)
        {
            var (products, totalCount) = await _productService.SearchProductAsync(
                query, categoryId, minPrice, maxPrice, sortBy, page, pageSize);

            return Ok(new
            {
                products,
                total = totalCount,
                page,
                pageSize,
                totalPages = (totalCount + pageSize - 1) / pageSize
            });
        }


        [HttpGet("products/{id}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetProductById(int id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product is null) return NotFound();
            return Ok(product);
        }

        [HttpPost("admin/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            var product = await _productService.CreateProductAsync(dto);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }


        [HttpPut("admin/products/{id}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto)
        {
            var result = await _productService.UpdateStockAsync(id, dto.Stock);
            if (!result) return NotFound();
            return Ok(new { message = "Stock updated successfully" });
        }


        [HttpDelete("admin/products/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            var result = await _productService.DeleteProductAsync(id);
            if (!result) return NotFound();
            return Ok(new { message = "Product deleted successfully" });
        }
    }

    public class UpdateStockDto
    {
        public int Stock { get; set; }
    }
}
