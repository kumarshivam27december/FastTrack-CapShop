using CapShop.AdminService.Application.Services;
using CapShop.AdminService.Contracts;
using CapShop.AdminService.Infrastructure;
using CapShop.AdminService.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace CapShop.AdminService.Tests;

public class AdminAppServiceTests
{
    private static AdminDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AdminDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AdminDbContext(options);
    }

    [Test]
    public async Task GetDashboardSummaryAsync_ComputesTotalsAndCachesResult()
    {
        await using var db = CreateDbContext();
        var orderRepo = new Mock<IOrderAdminRepository>();
        var catalogRepo = new Mock<ICatalogReadRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<AdminAppService>>();

        var now = DateTime.UtcNow;
        orderRepo.Setup(x => x.GetAllOrdersAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminOrderDto>
            {
                new() { Id = 1, OrderNumber = "ORD-1", Status = "Paid", TotalAmount = 100, CreatedAtUtc = now },
                new() { Id = 2, OrderNumber = "ORD-2", Status = "Cancelled", TotalAmount = 250, CreatedAtUtc = now.AddDays(-1) },
                new() { Id = 3, OrderNumber = "ORD-3", Status = "Packed", TotalAmount = 300, CreatedAtUtc = now }
            });
        catalogRepo.Setup(x => x.GetProductCountAsync(It.IsAny<CancellationToken>())).ReturnsAsync(21);

        var sut = new AdminAppService(orderRepo.Object, catalogRepo.Object, db, cache, logger.Object);

        var first = await sut.GetDashboardSummaryAsync("token", CancellationToken.None);
        var second = await sut.GetDashboardSummaryAsync("token", CancellationToken.None);

        Assert.That(first.TotalOrders, Is.EqualTo(3));
        Assert.That(first.OrdersToday, Is.EqualTo(2));
        Assert.That(first.RevenueTotal, Is.EqualTo(400));
        Assert.That(first.TotalProducts, Is.EqualTo(21));
        Assert.That(first.RecentOrders.Count, Is.EqualTo(3));
        Assert.That(second.TotalOrders, Is.EqualTo(3));

        orderRepo.Verify(x => x.GetAllOrdersAsync("token", It.IsAny<CancellationToken>()), Times.Once);
        catalogRepo.Verify(x => x.GetProductCountAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task UpdateOrderStatusAsync_WhenRepositorySucceeds_CreatesAuditLog()
    {
        await using var db = CreateDbContext();
        var orderRepo = new Mock<IOrderAdminRepository>();
        var catalogRepo = new Mock<ICatalogReadRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<AdminAppService>>();

        orderRepo.Setup(x => x.UpdateOrderStatusAsync(77, It.IsAny<UpdateOrderStatusAdminRequest>(), "bearer", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var sut = new AdminAppService(orderRepo.Object, catalogRepo.Object, db, cache, logger.Object);

        var ok = await sut.UpdateOrderStatusAsync(
            77,
            new UpdateOrderStatusAdminRequest { NewStatus = "Shipped", Notes = "Out for delivery" },
            "bearer",
            "admin@capshop.com",
            CancellationToken.None);

        Assert.That(ok, Is.True);
        Assert.That(await db.AdminAuditLogs.CountAsync(), Is.EqualTo(1));

        var log = await db.AdminAuditLogs.SingleAsync();
        Assert.That(log.EntityName, Is.EqualTo("Order"));
        Assert.That(log.EntityId, Is.EqualTo("77"));
        Assert.That(log.PerformedBy, Is.EqualTo("admin@capshop.com"));
        Assert.That(log.Notes, Does.Contain("Shipped"));
    }

    [Test]
    public async Task BuildSalesCsvAsync_IncludesHeaderAndRows()
    {
        await using var db = CreateDbContext();
        var orderRepo = new Mock<IOrderAdminRepository>();
        var catalogRepo = new Mock<ICatalogReadRepository>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var logger = new Mock<ILogger<AdminAppService>>();

        orderRepo.Setup(x => x.GetAllOrdersAsync("token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AdminOrderDto>
            {
                new()
                {
                    Id = 1,
                    OrderNumber = "ORD-1",
                    Status = "Paid",
                    TotalAmount = 120,
                    CreatedAtUtc = new DateTime(2026, 4, 8, 12, 0, 0, DateTimeKind.Utc)
                }
            });

        var sut = new AdminAppService(orderRepo.Object, catalogRepo.Object, db, cache, logger.Object);

        var csv = await sut.BuildSalesCsvAsync(new DateOnly(2026, 4, 1), new DateOnly(2026, 4, 30), "token", CancellationToken.None);

        Assert.That(csv, Does.StartWith("Date,OrderCount,Revenue"));
        Assert.That(csv, Does.Contain("2026-04-08,1,120"));
    }
}
