using CapShop.CatalogService.Application.Interfaces;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Infrastructure.Repositories;

namespace CapShop.CatalogService.Application.Services
{
    public class ProductAppService : IProductAppService
    {
        private readonly ICatalogRepository _repo;
        private readonly ILogger<ProductAppService> _logger;

        public ProductAppService(ICatalogRepository repo, ILogger<ProductAppService> logger)
        {
            _repo = repo;
            _logger = logger;
        }

        public Task<(List<ProductResponseDto> products, int totalCount)> SearchProductsAsync(
            string? query, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy,
            int page = 1, int pageSize = 10, CancellationToken ct = default)
            => _repo.SearchProductsAsync(query, categoryId, minPrice, maxPrice, sortBy, page, pageSize, ct);

        public Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken ct = default)
            => _repo.GetProductByIdAsync(id, ct);

        public Task<List<ProductResponseDto>> GetFeaturedProductsAsync(CancellationToken ct = default)
            => _repo.GetFeaturedProductsAsync(ct);

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(dto.Name)) throw new InvalidOperationException("Product name is required.");
            if (dto.Price <= 0) throw new InvalidOperationException("Price must be greater than 0.");
            if (dto.Stock < 0) throw new InvalidOperationException("Stock cannot be negative.");

            var result = await _repo.CreateProductAsync(dto, ct);
            _logger.LogInformation("Product created with id {ProductId}", result.Id);
            return result;
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
        {
            if (dto.Price <= 0) throw new InvalidOperationException("Price must be greater than 0.");
            if (dto.Stock < 0) throw new InvalidOperationException("Stock cannot be negative.");

            return await _repo.UpdateProductAsync(id, dto, ct);
        }

        public async Task<bool> UpdateStockAsync(int id, int quantity, CancellationToken ct = default)
        {
            if (quantity < 0) throw new InvalidOperationException("Stock cannot be negative.");
            return await _repo.UpdateStockAsync(id, quantity, ct);
        }

        public async Task<bool> DecreaseStockAsync(int id, int quantity, CancellationToken ct = default)
        {
            if (quantity <= 0) throw new InvalidOperationException("Quantity must be greater than 0.");
            return await _repo.DecreaseStockAsync(id, quantity, ct);
        }

        public Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
            => _repo.DeleteProductAsync(id, ct);
    }
}