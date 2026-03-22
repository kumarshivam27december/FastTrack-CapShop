using CapShop.CatalogService.DTOs.Catalog;
namespace CapShop.CatalogService.Services.Interfaces
{
    public interface IProductService
    {
        Task<(List<ProductResponseDto> products, int totalCount)> SearchProductAsync(
            string? query,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page = 1,
            int pageSize = 10
        );

        Task<ProductResponseDto?> GetProductByIdAsync(int id);
        Task<List<ProductResponseDto>> GetFeaturedProductsAsync();

        Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto);

        Task<bool> UpdateProductAsync(int id, UpdateProductDto dto);
        Task<bool> UpdateStockAsync(int id, int quantity);

        Task<bool> DeleteProductAsync(int id);
    }
}
