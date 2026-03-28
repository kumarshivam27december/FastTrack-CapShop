using CapShop.CatalogService.DTOs.Catalog;

namespace CapShop.CatalogService.Application.Interfaces
{
    public interface ICategoryAppService
    {
        Task<List<CategoryResponseDto>> GetAllCategoriesAsync(CancellationToken ct = default);
        Task<CategoryResponseDto?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
        Task<CategoryResponseDto> CreateCategoryAsync(CreateCategoryDto dto, CancellationToken ct = default);
        Task<bool> UpdateCategoryAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default);
        Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default);
    }
}
