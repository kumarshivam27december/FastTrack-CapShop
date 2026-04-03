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
        private const string CacheVersionKey = "catalog:version";
        private const string CategoryListCacheKey = "catalog:categories:all";
        private const string CategoryItemCachePrefix = "catalog:category";

        public CategoryRepository(CatalogDbContext dbContext, IDistributedCache cache)
        {
            _dbContext = dbContext;
            _cache = cache;
        }

        public async Task<List<Category>> GetAllAsync(CancellationToken ct = default)
        {
            var version = await GetCacheVersionAsync(ct);
            var cacheKey = BuildKey(version, CategoryListCacheKey);
            var cached = await _cache.GetStringAsync(cacheKey, ct);
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
                cacheKey,
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
            var version = await GetCacheVersionAsync(ct);
            var cacheKey = BuildKey(version, $"{CategoryItemCachePrefix}:{id}");
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
            await InvalidateCatalogCacheAsync(ct);
            return category;
        }

        public async Task UpdateAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
        }

        public async Task DeleteAsync(Category category, CancellationToken ct = default)
        {
            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
        }

        private async Task<string> GetCacheVersionAsync(CancellationToken ct)
        {
            var version = await _cache.GetStringAsync(CacheVersionKey, ct);
            if (!string.IsNullOrWhiteSpace(version))
            {
                return version;
            }

            version = "1";
            await _cache.SetStringAsync(
                CacheVersionKey,
                version,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                },
                ct);

            return version;
        }

        private async Task InvalidateCatalogCacheAsync(CancellationToken ct)
        {
            await _cache.SetStringAsync(
                CacheVersionKey,
                Guid.NewGuid().ToString("N"),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(1)
                },
                ct);
        }

        private static string BuildKey(string version, string suffix)
            => $"catalog:{version}:{suffix}";
    }
}
