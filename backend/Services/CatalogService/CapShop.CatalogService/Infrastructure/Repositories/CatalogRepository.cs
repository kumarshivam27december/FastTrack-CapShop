using CapShop.CatalogService.Data;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly CatalogDbContext _db;
        private readonly IDistributedCache _cache;
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private const string CacheVersionKey = "catalog:version";
        private const string ProductCachePrefix = "catalog:product";
        private const string FeaturedCacheKey = "catalog:featured";
        private const string SearchCachePrefix = "catalog:search";

        public CatalogRepository(CatalogDbContext db, IDistributedCache cache)
        {
            _db = db;
            _cache = cache;
        }

        public async Task<(List<ProductResponseDto> products, int totalCount)> SearchProductsAsync(
            string? query, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy,
            int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
            var cacheVersion = await GetCacheVersionAsync(ct);
            var cacheKey = BuildSearchCacheKey(cacheVersion, query, categoryId, minPrice, maxPrice, sortBy, page, pageSize);
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var cachedResult = JsonSerializer.Deserialize<SearchProductsCacheEntry>(cached, JsonOptions);
                if (cachedResult is not null)
                {
                    return (cachedResult.Products, cachedResult.TotalCount);
                }
            }

            var q = _db.Products.AsQueryable();

            if (!string.IsNullOrWhiteSpace(query))
                q = q.Where(x => x.Name.Contains(query) || x.Description.Contains(query));

            if (categoryId.HasValue)
                q = q.Where(x => x.CategoryId == categoryId.Value);

            if (minPrice.HasValue)
                q = q.Where(x => x.Price >= minPrice.Value);

            if (maxPrice.HasValue)
                q = q.Where(x => x.Price <= maxPrice.Value);

            q = q.Where(x => x.IsActive);

            q = sortBy?.ToLowerInvariant() switch
            {
                "price_asc" => q.OrderBy(x => x.Price),
                "price_desc" => q.OrderByDescending(x => x.Price),
                "newest" => q.OrderByDescending(x => x.CreatedAtUtc),
                _ => q.OrderBy(x => x.Name)
            };

            var totalCount = await q.CountAsync(ct);

            var products = await q
                .Include(x => x.Category)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync(ct);

            var result = (products: products.Select(MapToDto).ToList(), totalCount);

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(new SearchProductsCacheEntry(result.products, result.totalCount), JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                },
                ct);

            return result;
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken ct = default)
        {
            var cacheVersion = await GetCacheVersionAsync(ct);
            var cacheKey = BuildProductCacheKey(cacheVersion, id);
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                return JsonSerializer.Deserialize<ProductResponseDto>(cached, JsonOptions);
            }

            var product = await _db.Products
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

            if (product is null)
            {
                return null;
            }

            var mapped = MapToDto(product);
            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(mapped, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
                },
                ct);

            return mapped;
        }

        public async Task<List<ProductResponseDto>> GetFeaturedProductsAsync(CancellationToken ct = default)
        {
            var cacheVersion = await GetCacheVersionAsync(ct);
            var cacheKey = BuildKey(cacheVersion, FeaturedCacheKey);
            var cached = await _cache.GetStringAsync(cacheKey, ct);
            if (!string.IsNullOrWhiteSpace(cached))
            {
                var cachedFeatured = JsonSerializer.Deserialize<List<ProductResponseDto>>(cached, JsonOptions);
                if (cachedFeatured is not null)
                {
                    return cachedFeatured;
                }
            }

            var featured = await _db.Products
                .Where(x => x.IsActive && x.Stock > 0)
                .Include(x => x.Category)
                .Take(6)
                .ToListAsync(ct);

            var result = featured.Select(MapToDto).ToList();

            await _cache.SetStringAsync(
                cacheKey,
                JsonSerializer.Serialize(result, JsonOptions),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(2)
                },
                ct);

            return result;
        }

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto, CancellationToken ct = default)
        {
            var product = new Product
            {
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                Stock = dto.Stock,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                IsActive = true
            };

            _db.Products.Add(product);
            await _db.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);

            var created = await _db.Products
                .Include(x => x.Category)
                .FirstAsync(x => x.Id == product.Id, ct);

            return MapToDto(created);
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto, CancellationToken ct = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, ct);
            if (product is null) return false;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            await _db.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
            return true;
        }

        public async Task<bool> UpdateStockAsync(int id, int quantity, CancellationToken ct = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, ct);
            if (product is null) return false;

            product.Stock = quantity;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
            return true;
        }

        public async Task<bool> DecreaseStockAsync(int id, int quantity, CancellationToken ct = default)
        {
            if (quantity <= 0) return false;

            var affectedRows = await _db.Products
                .Where(x => x.Id == id && x.Stock >= quantity)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(x => x.Stock, x => x.Stock - quantity)
                    .SetProperty(x => x.UpdatedAtUtc, _ => DateTime.UtcNow), ct);

            if (affectedRows == 0)
            {
                return false;
            }

            await InvalidateCatalogCacheAsync(ct);
            return true;
        }

        public async Task<bool> ReserveStockAsync(IEnumerable<CapShop.Shared.Events.OrderPlacedItemEvent> items, CancellationToken ct = default)
        {
            var stockItems = items
                .Where(x => x.ProductId > 0 && x.Quantity > 0)
                .ToList();

            if (!stockItems.Any())
            {
                return false;
            }

            await using var transaction = await _db.Database.BeginTransactionAsync(ct);

            foreach (var item in stockItems)
            {
                var product = await _db.Products.FirstOrDefaultAsync(x => x.Id == item.ProductId, ct);
                if (product is null || !product.IsActive || product.Stock < item.Quantity)
                {
                    await transaction.RollbackAsync(ct);
                    return false;
                }

                product.Stock -= item.Quantity;
                product.UpdatedAtUtc = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, ct);
            if (product is null) return false;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync(ct);
            await InvalidateCatalogCacheAsync(ct);
            return true;
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

        private static string BuildProductCacheKey(string version, int id)
            => BuildKey(version, $"{ProductCachePrefix}:{id}");

        private static string BuildSearchCacheKey(
            string version,
            string? query,
            int? categoryId,
            decimal? minPrice,
            decimal? maxPrice,
            string? sortBy,
            int page,
            int pageSize)
        {
            var rawKey = $"q={query ?? string.Empty}|c={categoryId?.ToString() ?? string.Empty}|min={minPrice?.ToString() ?? string.Empty}|max={maxPrice?.ToString() ?? string.Empty}|sort={sortBy ?? string.Empty}|page={page}|size={pageSize}";
            var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(rawKey))).ToLowerInvariant();
            return BuildKey(version, $"{SearchCachePrefix}:{hash}");
        }

        private static string BuildKey(string version, string suffix)
            => $"catalog:{version}:{suffix}";

        private static ProductResponseDto MapToDto(Product product)
        {
            return new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                Stock = product.Stock,
                ImageUrl = product.ImageUrl,
                Category = new CategoryResponseDto
                {
                    Id = product.Category.Id,
                    Name = product.Category.Name,
                    Description = product.Category.Description
                }
            };
        }

        private sealed record SearchProductsCacheEntry(List<ProductResponseDto> Products, int TotalCount);
    }
}