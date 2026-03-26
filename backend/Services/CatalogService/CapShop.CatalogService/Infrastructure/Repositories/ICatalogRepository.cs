using CapShop.CatalogService.DTOs.Catalog;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public interface ICatalogRepository
    {
        Task<(List<ProductResponseDto> products, int totalCount)> SearchProductsAsync(
            string? query,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1,
            int pageSize = 10,
            CancellationToken ct = default);

        Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken ct = default);
        Task<List<ProductResponseDto>> GetFeaturedProductsAsync(CancellationToken ct = default);
        Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default);
        Task<bool> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default);
        Task<bool> UpdateStockAsync(int id, int quantity, CancellationToken ct = default);
        Task<bool> DeleteProductAsync(int id, CancellationToken ct = default);
    }
}