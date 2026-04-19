using CapShop.CatalogService.Data;
using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Text.Json;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly CatalogDbContext _dbContext;
        private readonly IDistributedCache _cache;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string CategoryListCacheKey = "catalog:categories:all";
        private const string CategoryItemCachePrefix = "catalog:category";
        private const string FeaturedCacheKey = "catalog:featured";
        private const string SearchCacheVersionKey = "catalog:version:search";

        public CategoryRepository(CatalogDbContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        {
            var cached = await _cache.GetStringAsync(CategoryListCacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var categories = JsonSerializer.Deserialize<List<Category>>(cached, JsonOptions);
                if (categories is not null)
                {
                    return categories;
                }
            }

            var result = await _dbContext.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .ToListAsync(ct);

            await _cache.SetStringAsync(
                CategoryListCacheKey,
                JsonSerializer.Serialize(result, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                ct);

            return result;
        }

        public async Task<Category?> GetByIdAsync(int id, CancellationToken ct = default)
        {
            var cacheKey = BuildCategoryItemCacheKey(id);
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return JsonSerializer.Deserialize<Category>(cached, JsonOptions);
            }

            var category = await _dbContext.Categories
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id, ct);

            if (category is null)
            {
                return null;
            }

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(category, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                ct);

            return category;
        }

        public async Task<Category> AddAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync(ct);
            await InvalidateCategoryCachesAsync(category.Id, ct);
            return category;
        }

        public async Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync(ct);
            await InvalidateCategoryCachesAsync(category.Id, ct);
        }

        public async Task DeleteAsync(Category category, CancellationToken ct = default)
        {
            var deletedCategoryId = category.Id;
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync(ct);
            await InvalidateCategoryCachesAsync(deletedCategoryId, ct);
        }

        private static string BuildCategoryItemCacheKey(int id)
            => $"{CategoryItemCachePrefix}:{id}";

        private async Task InvalidateCategoryCachesAsync(int categoryId, CancellationToken ct)
        {
            await _cache.RemoveAsync(CategoryListCacheKey, ct);

            if (categoryId > 0)
            {
                await _cache.RemoveAsync(BuildCategoryItemCacheKey(categoryId), ct);
            }

            // Category metadata can affect featured/search payloads (category name/description in DTOs).
            await _cache.RemoveAsync(FeaturedCacheKey, ct);
            await _cache.SetStringAsync(
                SearchCacheVersionKey,
                Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                },
                ct);
        }
    }
}
