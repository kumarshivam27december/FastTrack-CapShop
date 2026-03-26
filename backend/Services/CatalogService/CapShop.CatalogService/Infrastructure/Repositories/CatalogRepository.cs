using CapShop.CatalogService.Data;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace CapShop.CatalogService.Infrastructure.Repositories
{
    public class CatalogRepository : ICatalogRepository
    {
        private readonly CatalogDbContext _db;

        public CatalogRepository(CatalogDbContext db)
        {
            _db = db;
        }

        public async Task<(List<ProductResponseDto> products, int totalCount)> SearchProductsAsync(
            string? query, int? categoryId, decimal? minPrice, decimal? maxPrice, string? sortBy,
            int page = 1, int pageSize = 10, CancellationToken ct = default)
        {
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

            return (products.Select(MapToDto).ToList(), totalCount);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id, CancellationToken ct = default)
        {
            var product = await _db.Products
                .Include(x => x.Category)
                .FirstOrDefaultAsync(x => x.Id == id && x.IsActive, ct);

            return product is null ? null : MapToDto(product);
        }

        public async Task<List<ProductResponseDto>> GetFeaturedProductsAsync(CancellationToken ct = default)
        {
            var featured = await _db.Products
                .Where(x => x.IsActive && x.Stock > 0)
                .Include(x => x.Category)
                .Take(6)
                .ToListAsync(ct);

            return featured.Select(MapToDto).ToList();
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
            return true;
        }

        public async Task<bool> UpdateStockAsync(int id, int quantity, CancellationToken ct = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, ct);
            if (product is null) return false;

            product.Stock = quantity;
            product.UpdatedAtUtc = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id, CancellationToken ct = default)
        {
            var product = await _db.Products.FindAsync(new object[] { id }, ct);
            if (product is null) return false;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync(ct);
            return true;
        }

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
    }
}