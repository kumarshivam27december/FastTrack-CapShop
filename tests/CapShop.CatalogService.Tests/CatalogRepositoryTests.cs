using CapShop.CatalogService.Data;
using CapShop.CatalogService.DTOs.Catalog;
using CapShop.CatalogService.Infrastructure.Repositories;
using CapShop.CatalogService.Models;
using CapShop.Shared.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CapShop.CatalogService.Tests;

public class CatalogRepositoryTests
{
    private static CatalogDbContext CreateDbContext(out SqliteConnection connection)
    {
        connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<CatalogDbContext>()
            .UseSqlite(connection)
            .Options;

        var db = new CatalogDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static IDistributedCache CreateCache()
    {
        return new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()));
    }

    [Test]
    public async Task SearchProductsAsync_FiltersToActiveAndReturnsCount()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        var category = new Category { Name = "Caps", Description = "Headwear" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        db.Products.AddRange(
            new Product { Name = "Alpha Cap", Description = "A", Price = 100, Stock = 5, CategoryId = category.Id, IsActive = true, ImageUrl = "x" },
            new Product { Name = "Beta Cap", Description = "B", Price = 200, Stock = 0, CategoryId = category.Id, IsActive = true, ImageUrl = "x" },
            new Product { Name = "Old Cap", Description = "C", Price = 300, Stock = 1, CategoryId = category.Id, IsActive = false, ImageUrl = "x" });
        await db.SaveChangesAsync();

        var sut = new CatalogRepository(db, cache);

        var (products, total) = await sut.SearchProductsAsync("Cap", null, null, null, "newest", 1, 10);

        Assert.That(total, Is.EqualTo(2));
        Assert.That(products.Count, Is.EqualTo(2));
        Assert.That(products.All(p => p.Name.Contains("Cap")), Is.True);
    }

    [Test]
    public async Task DecreaseStockAsync_WhenStockSufficient_ReturnsTrueAndDecrements()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        var category = new Category { Name = "Caps", Description = "Headwear" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = "Stock Cap",
            Description = "Cap",
            Price = 150,
            Stock = 10,
            CategoryId = category.Id,
            IsActive = true,
            ImageUrl = "img"
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sut = new CatalogRepository(db, cache);

        var ok = await sut.DecreaseStockAsync(product.Id, 3);
        await db.Entry(product).ReloadAsync();

        Assert.That(ok, Is.True);
        Assert.That(product.Stock, Is.EqualTo(7));
    }

    [Test]
    public async Task ReserveStockAsync_WhenAnyItemInsufficient_ReturnsFalse()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        var category = new Category { Name = "Caps", Description = "Headwear" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var product = new Product
        {
            Name = "Low Stock Cap",
            Description = "Cap",
            Price = 150,
            Stock = 1,
            CategoryId = category.Id,
            IsActive = true,
            ImageUrl = "img"
        };

        db.Products.Add(product);
        await db.SaveChangesAsync();

        var sut = new CatalogRepository(db, cache);

        var ok = await sut.ReserveStockAsync(new List<OrderPlacedItemEvent>
        {
            new TestOrderPlacedItemEvent
            {
                ProductId = product.Id,
                Quantity = 2,
                Price = 150,
                Amount = 300,
                Title = "Low Stock Cap",
                Description = "Cap"
            }
        });

        Assert.That(ok, Is.False);
    }

    [Test]
    public async Task CreateProductAsync_PersistsAndReturnsMappedDto()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        var category = new Category { Name = "Caps", Description = "Headwear" };
        db.Categories.Add(category);
        await db.SaveChangesAsync();

        var sut = new CatalogRepository(db, cache);

        var result = await sut.CreateProductAsync(new CreateProductDto
        {
            Name = "New Cap",
            Description = "Desc",
            Price = 500,
            Stock = 8,
            ImageUrl = "img",
            CategoryId = category.Id
        });

        Assert.That(result.Id, Is.GreaterThan(0));
        Assert.That(result.Category.Name, Is.EqualTo("Caps"));
    }

    private sealed class TestOrderPlacedItemEvent : OrderPlacedItemEvent
    {
        public int ProductId { get; init; }
        public string Title { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Price { get; init; }
        public int Quantity { get; init; }
        public decimal Amount { get; init; }
    }
}
