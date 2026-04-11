using CapShop.CatalogService.Data;
using CapShop.CatalogService.Infrastructure.Repositories;
using CapShop.CatalogService.Models;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace CapShop.CatalogService.Tests;

public class CategoryRepositoryTests
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
    public async Task GetAllAsync_ReturnsCategoriesOrderedByName()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        db.Categories.AddRange(
            new Category { Name = "Zeta", Description = "d" },
            new Category { Name = "Alpha", Description = "d" });
        await db.SaveChangesAsync();

        var sut = new CategoryRepository(db, cache);

        var list = await sut.GetAllAsync();

        Assert.That(list.Count, Is.EqualTo(2));
        Assert.That(list[0].Name, Is.EqualTo("Alpha"));
        Assert.That(list[1].Name, Is.EqualTo("Zeta"));
    }

    [Test]
    public async Task AddAsync_PersistsCategory()
    {
        await using var db = CreateDbContext(out var connection);
        await using var _ = connection;
        var cache = CreateCache();

        var sut = new CategoryRepository(db, cache);

        var created = await sut.AddAsync(new Category
        {
            Name = "Sports",
            Description = "Sports caps"
        });

        Assert.That(created.Id, Is.GreaterThan(0));
        Assert.That(await db.Categories.CountAsync(), Is.EqualTo(1));
    }
}
