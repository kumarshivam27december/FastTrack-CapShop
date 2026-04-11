using CapShop.OrderService.Data;
using CapShop.OrderService.DTOs.Address;
using CapShop.OrderService.Infrastructure.Repositories;
using CapShop.OrderService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using System.Net;
using System.Net.Http;

namespace CapShop.OrderService.Tests;

public class OrderRepositoryCoreTests
{
    private static OrderDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new OrderDbContext(options);
    }

    private static OrderRepository CreateSut(OrderDbContext db)
    {
        var httpFactory = new Mock<IHttpClientFactory>();
        httpFactory.Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler()));

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CatalogServiceUrl"] = "http://localhost:5014",
                ["PaymentServiceUrl"] = "http://localhost:5017"
            })
            .Build();

        var publish = new Mock<IPublishEndpoint>();

        return new OrderRepository(db, httpFactory.Object, configuration, publish.Object);
    }

    [Test]
    public async Task GetOrCreateCartAsync_WhenMissing_CreatesCart()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var cart = await sut.GetOrCreateCartAsync(10);

        Assert.That(cart.CartId, Is.GreaterThan(0));
        Assert.That(await db.Carts.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void SaveAddressAsync_WhenPincodeInvalid_Throws()
    {
        using var db = CreateDbContext();
        var sut = CreateSut(db);

        var action = async () => await sut.SaveAddressAsync(1, new AddressRequestDto
        {
            FullName = "User",
            Street = "Street",
            City = "City",
            State = "State",
            Pincode = "12AB",
            Phone = "9999999999"
        });

        Assert.ThrowsAsync<InvalidOperationException>(async () => await action());
    }

    [Test]
    public async Task SaveAddressAsync_WhenValid_PersistsAddress()
    {
        await using var db = CreateDbContext();
        var sut = CreateSut(db);

        var address = await sut.SaveAddressAsync(1, new AddressRequestDto
        {
            FullName = "User",
            Street = "Street",
            City = "City",
            State = "State",
            Pincode = "560001",
            Phone = "9999999999"
        });

        Assert.That(address.Id, Is.GreaterThan(0));
        Assert.That(await db.Addresses.CountAsync(), Is.EqualTo(1));
    }

    [Test]
    public void CancelOrderAsync_WhenAlreadyShipped_Throws()
    {
        using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            Id = 50,
            UserId = 4,
            OrderNumber = "ORD-50",
            Status = OrderStatus.Shipped,
            TotalAmount = 99
        });
        db.SaveChanges();

        var sut = CreateSut(db);

        var action = async () => await sut.CancelOrderAsync(50, 4);

        Assert.ThrowsAsync<InvalidOperationException>(async () => await action());
    }

    [Test]
    public async Task UpdateOrderStatusAsync_WhenTransitionInvalid_ReturnsFalse()
    {
        await using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            Id = 60,
            UserId = 4,
            OrderNumber = "ORD-60",
            Status = OrderStatus.Paid,
            TotalAmount = 99
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var updated = await sut.UpdateOrderStatusAsync(60, "Shipped");

        Assert.That(updated, Is.False);
    }

    [Test]
    public async Task CancelOrderAsync_WhenAllowed_CancelsOrderAndWritesHistory()
    {
        await using var db = CreateDbContext();
        db.Orders.Add(new Order
        {
            Id = 70,
            UserId = 4,
            OrderNumber = "ORD-70",
            Status = OrderStatus.Paid,
            TotalAmount = 99
        });
        await db.SaveChangesAsync();

        var sut = CreateSut(db);

        var cancelled = await sut.CancelOrderAsync(70, 4);

        var order = await db.Orders.FindAsync(70);

        Assert.That(cancelled, Is.True);
        Assert.That(order!.Status, Is.EqualTo(OrderStatus.Cancelled));
        Assert.That(await db.OrderStatusHistories.AnyAsync(x => x.OrderId == 70 && x.ToStatus == OrderStatus.Cancelled), Is.True);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
            {
                Content = new StringContent("{}")
            });
        }
    }
}
