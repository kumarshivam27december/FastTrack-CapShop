using CapShop.CatalogService.Models;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public interface ICategoryRepository
    {
        Task<List<Category>> GetAllAsync(CancellationToken ct = default);
        Task<Category?> GetByIdAsync(int id, CancellationToken ct = default);
        Task<Category> AddAsync(Category category, CancellationToken ct = default);
        Task UpdateAsync(Category category, CancellationToken ct = default);
        Task DeleteAsync(Category category, CancellationToken ct = default);
    }
}
