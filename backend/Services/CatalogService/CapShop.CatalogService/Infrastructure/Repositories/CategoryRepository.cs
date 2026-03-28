using CapShop.CatalogService.Data;
using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CatalogDbContext _dbContext;

        public CategoryRepository(CatalogDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);
        }

        public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(ct);
            return category;
        }

        public async Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync(ct);
        }
    }
}
