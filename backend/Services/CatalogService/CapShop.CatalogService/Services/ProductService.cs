using CapShop.CatalogService.Data;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Services.Interfaces;
using CapShop.CatalogService.Models;
using Microsoft.EntityFrameworkCore;
namespace CapShop.CatalogService.Services
{
    public class ProductService : IProductService
    {
        private readonly CatalogDbContext _db;

        public ProductService(CatalogDbContext db)
        {
            _db = db;
        }

        public async Task<(List<ProductResponseDto>,int)> SearchProductAsync(string? query, int? categoryId, decimal? minPrice, decimal? maxPrice,string? sortBy, int page = 1, int pageSize = 10)
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

            var totalCount = await q.CountAsync();
            var products = await q
                .Include(x => x.Category)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = products.Select(MapToDto).ToList();
            return (dtos, totalCount);
        }

        public async Task<ProductResponseDto?> GetProductByIdAsync(int id)
        {
            var product = await _db.Products.Include(x => x.Category).FirstOrDefaultAsync(x => x.Id == id && x.IsActive);
            return product is null ? null : MapToDto(product);
        }

        public async Task<List<ProductResponseDto>> GetFeaturedProductsAsync()
        {
            var featured = await _db.Products
                .Where(x => x.IsActive && x.Stock > 0)
                .Include(x => x.Category)
                .Take(6)
                .ToListAsync();

            return featured.Select(MapToDto).ToList();
        }

        public async Task<ProductResponseDto> CreateProductAsync(CreateProductDto dto)
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
            await _db.SaveChangesAsync();

            // Reload with Category included
            var createdProduct = await _db.Products
                .Include(x => x.Category)
                .FirstAsync(x => x.Id == product.Id);

            return MapToDto(createdProduct);
        }

        public async Task<bool> UpdateProductAsync(int id, UpdateProductDto dto)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null) return false;

            product.Name = dto.Name;
            product.Description = dto.Description;
            product.Price = dto.Price;
            product.Stock = dto.Stock;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.IsActive = dto.IsActive;
            product.UpdatedAtUtc = DateTime.UtcNow;

            _db.Products.Update(product);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> UpdateStockAsync(int id, int quantity)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null) return false;

            product.Stock = quantity;
            product.UpdatedAtUtc = DateTime.UtcNow;
            _db.Products.Update(product);
            await _db.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            var product = await _db.Products.FindAsync(id);
            if (product is null) return false;

            _db.Products.Remove(product);
            await _db.SaveChangesAsync();
            return true;
        }

        private ProductResponseDto MapToDto(Product product)
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
                    Id = product.Category!.Id,
                    Name = product.Category.Name,
                    Description = product.Category.Description
                }
            };
        }

       
    }
}
