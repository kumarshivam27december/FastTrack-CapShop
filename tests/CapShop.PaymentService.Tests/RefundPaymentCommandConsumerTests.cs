using CapShop.PaymentService.Consumers;
using CapShop.PaymentService.Data;
using CapShop.PaymentService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CapShop.PaymentService.Tests;

public class RefundPaymentCommandConsumerTests
{
    private static PaymentDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new PaymentDbContext(options);
    }

    [Test]
    public async Task Consume_WhenSucceededPaymentExists_UpdatesStatusToRefundedAndPublishesRefundedEvent()
    {
        await using var db = CreateDbContext();
        db.Payments.Add(new PaymentRecord
        {
            OrderId = 501,
            UserId = 8,
            Amount = 50m,
            Currency = "INR",
            PaymentMethod = "UPI",
            Status = PaymentStatus.Succeeded,
            TransactionId = "TXN-123"
        });
        await db.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        var sut = new RefundPaymentCommandConsumer(db, publish.Object);

        var message = new TestRefundPaymentCommand
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 501,
            UserId = 8,
            UserEmail = "buyer@capshop.com",
            OrderNumber = "ORD-501",
            Amount = 50m,
            TransactionId = "TXN-123",
            Reason = "Stock reservation failed",
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<RefundPaymentCommand>>();
        ctx.SetupGet(x => x.Message).Returns(message);
        ctx.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await sut.Consume(ctx.Object);

        var saved = await db.Payments.SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(PaymentStatus.Refunded));

        publish.Verify(x => x.Publish<PaymentRefundedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Once);
        publish.Verify(x => x.Publish<PaymentRefundFailedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task Consume_WhenPaymentAlreadyRefunded_DoesNotPublishAgain()
    {
        await using var db = CreateDbContext();
        db.Payments.Add(new PaymentRecord
        {
            OrderId = 777,
            UserId = 20,
            Amount = 99m,
            Currency = "INR",
            PaymentMethod = "Card",
            Status = PaymentStatus.Refunded,
            TransactionId = "TXN-777"
        });
        await db.SaveChangesAsync();

        var publish = new Mock<IPublishEndpoint>();
        var sut = new RefundPaymentCommandConsumer(db, publish.Object);

        var message = new TestRefundPaymentCommand
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 777,
            UserId = 20,
            UserEmail = "buyer@capshop.com",
            OrderNumber = "ORD-777",
            Amount = 99m,
            TransactionId = "TXN-777",
            Reason = "Stock reservation failed",
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<RefundPaymentCommand>>();
        ctx.SetupGet(x => x.Message).Returns(message);
        ctx.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);

        await sut.Consume(ctx.Object);

        publish.Verify(x => x.Publish<PaymentRefundedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        publish.Verify(x => x.Publish<PaymentRefundFailedEvent>(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private sealed class TestRefundPaymentCommand : RefundPaymentCommand
    {
        public Guid CorrelationId { get; init; }
        public int OrderId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public string OrderNumber { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public string? TransactionId { get; init; }
        public string Reason { get; init; } = string.Empty;
        public DateTime OccurredAtUtc { get; init; }
    }
}
