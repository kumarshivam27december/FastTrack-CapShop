using CapShop.PaymentService.Consumers;
using CapShop.PaymentService.Data;
using CapShop.PaymentService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CapShop.PaymentService.Tests;

public class OrderCreatedEventConsumerTests
{
    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    [Test]
    public async Task Consume_WhenSimulateSuccess_CreatesSucceededPaymentAndPublishesSuccessEvent()
    {
        await using var db = CreateDbContext();
        var publish = new Mock<IPublishEndpoint>();

        var sut = new OrderCreatedEventConsumer(db, publish.Object);
        var message = new TestOrderCreatedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 101,
            UserId = 7,
            UserEmail = "buyer@capshop.com",
            TotalAmount = 1200,
            OrderNumber = "ORD-101",
            PaymentMethod = "UPI",
            SimulateSuccess = true,
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<OrderCreatedEvent>>();
        ctx.SetupGet(x => x.Message).Returns(message);

        await sut.Consume(ctx.Object);

        var saved = await db.Payments.SingleAsync();
        Assert.That(saved.OrderId, Is.EqualTo(101));
        Assert.That(saved.Status, Is.EqualTo(PaymentStatus.Succeeded));
        Assert.That(saved.TransactionId, Is.Not.Null.And.Not.Empty);

        publish.Verify(x => x.Publish<PaymentSucceededEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        publish.Verify(x => x.Publish<PaymentFailedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Consume_WhenSimulateFailure_CreatesFailedPaymentAndPublishesFailedEvent()
    {
        await using var db = CreateDbContext();
        var publish = new Mock<IPublishEndpoint>();

        var sut = new OrderCreatedEventConsumer(db, publish.Object);
        var message = new TestOrderCreatedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 202,
            UserId = 8,
            UserEmail = "buyer@capshop.com",
            TotalAmount = 550,
            OrderNumber = "ORD-202",
            PaymentMethod = "Card",
            SimulateSuccess = false,
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<OrderCreatedEvent>>();
        ctx.SetupGet(x => x.Message).Returns(message);

        await sut.Consume(ctx.Object);

        var saved = await db.Payments.SingleAsync();
        Assert.That(saved.OrderId, Is.EqualTo(202));
        Assert.That(saved.Status, Is.EqualTo(PaymentStatus.Failed));
        Assert.That(saved.TransactionId, Is.Null);
        Assert.That(saved.FailureReason, Is.EqualTo("Simulated payment failure"));

        publish.Verify(x => x.Publish<PaymentFailedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        publish.Verify(x => x.Publish<PaymentSucceededEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestOrderCreatedEvent : OrderCreatedEvent
    {
        public Guid CorrelationId { get; init; }
        public int OrderId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public string OrderNumber { get; init; } = string.Empty;
        public string PaymentMethod { get; init; } = string.Empty;
        public bool SimulateSuccess { get; init; }
        public DateTime OccurredAtUtc { get; init; }
    }
}
