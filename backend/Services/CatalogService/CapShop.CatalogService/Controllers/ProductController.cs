using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.Shared.Events;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CapShop.CatalogService.Controllers
{
    [ApiController]
    [Route("catalog")]
    public class ProductController : ControllerBase
    {
        private readonly IProductAppService _productAppService;

        public ProductController(IProductAppService productAppService)
        {
            _productAppService = productAppService;
        }

        [HttpGet("health")]
        [AllowAnonymous]
        public IActionResult Health() => Ok(new { service = "CatalogService", status = "Healthy" });

        [HttpGet("featured")]
        [AllowAnonymous]
        public async Task<IActionResult> GetFeatured(CancellationToken ct)
        {
            var featured = await _productAppService.GetFeaturedProductsAsync(ct);
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
            [FromQuery] int pageSize = 10,
            CancellationToken ct = default)
        {
            var (products, totalCount) = await _productAppService.SearchProductsAsync(
                query, categoryId, minPrice, maxPrice, sortBy, page, pageSize, ct);

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
        public async Task<IActionResult> GetProductById(int id, CancellationToken ct)
        {
            var product = await _productAppService.GetProductByIdAsync(id, ct);
            if (product is null) return NotFound();
            return Ok(product);
        }

        [HttpPost("admin/products")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateProduct([FromBody] CreateProductDto dto, CancellationToken ct)
        {
            var product = await _productAppService.CreateProductAsync(dto, ct);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id }, product);
        }

        [HttpPut("admin/products/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateProductDto dto, CancellationToken ct)
        {
            var ok = await _productAppService.UpdateProductAsync(id, dto, ct);
            if (!ok) return NotFound();
            return Ok(new { message = "Product updated successfully" });
        }

        [HttpPut("admin/products/{id}/stock")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] UpdateStockDto dto, CancellationToken ct)
        {
            var ok = await _productAppService.UpdateStockAsync(id, dto.Stock, ct);
            if (!ok) return NotFound();
            return Ok(new { message = "Stock updated successfully" });
        }

        [HttpDelete("admin/products/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
        {
            var ok = await _productAppService.DeleteProductAsync(id, ct);
            if (!ok) return NotFound();
            return Ok(new { message = "Product deleted successfully" });
        }

        [HttpPost("internal/reserve-stock")]
        [AllowAnonymous]
        public async Task<IActionResult> ReserveStock([FromBody] List<InternalReserveStockItemDto> items, CancellationToken ct)
        {
            if (items is null || items.Count == 0)
            {
                return BadRequest(new { message = "At least one item is required." });
            }

            var mappedItems = items
                .Where(x => x.ProductId > 0 && x.Quantity > 0)
                .Select(x => new InternalOrderPlacedItemEvent
                {
                    ProductId = x.ProductId,
                    Title = x.Title ?? string.Empty,
                    Description = x.Description ?? string.Empty,
                    Price = x.Price,
                    Quantity = x.Quantity,
                    Amount = x.Amount
                })
                .Cast<OrderPlacedItemEvent>()
                .ToList();

            if (mappedItems.Count == 0)
            {
                return BadRequest(new { message = "No valid stock items were provided." });
            }

            var reserved = await _productAppService.ReserveStockAsync(mappedItems, ct);
            return Ok(new { success = reserved });
        }
    }

    public class UpdateStockDto
    {
        public int Stock { get; set; }
    }

    public class InternalReserveStockItemDto
    {
        public int ProductId { get; set; }
        public string? Title { get; set; }
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }

    internal class InternalOrderPlacedItemEvent : OrderPlacedItemEvent
    {
        public int ProductId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public decimal Amount { get; set; }
    }
}