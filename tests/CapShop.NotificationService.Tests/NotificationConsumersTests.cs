using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.Consumers;
using CapShop.NotificationService.Data;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace CapShop.NotificationService.Tests;

public class NotificationConsumersTests
{
    private static NotificationDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new NotificationDbContext(options);
    }

    [Test]
    public async Task PaymentSucceededConsumer_WhenEmailMissing_StoresFailedNotification()
    {
        await using var db = CreateDbContext();
        var emailSender = new Mock<IEmailSender>();
        var sut = new PaymentSucceededEventConsumer(db, emailSender.Object);

        var msg = new TestPaymentSucceededEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 11,
            UserId = 1,
            UserEmail = " ",
            PaymentId = 34,
            TransactionId = "TXN-1",
            Amount = 99,
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<PaymentSucceededEvent>>();
        ctx.SetupGet(x => x.Message).Returns(msg);

        await sut.Consume(ctx.Object);

        var saved = await db.Notifications.SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(NotificationStatus.Failed));
        Assert.That(saved.ErrorMessage, Is.EqualTo("User email is missing in event payload."));

        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task PaymentSucceededConsumer_WhenEmailSendSucceeds_StoresSentNotification()
    {
        await using var db = CreateDbContext();
        var emailSender = new Mock<IEmailSender>();
        var sut = new PaymentSucceededEventConsumer(db, emailSender.Object);

        var msg = new TestPaymentSucceededEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 12,
            UserId = 2,
            UserEmail = "buyer@capshop.com",
            PaymentId = 35,
            TransactionId = "TXN-2",
            Amount = 149,
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<PaymentSucceededEvent>>();
        ctx.SetupGet(x => x.Message).Returns(msg);

        await sut.Consume(ctx.Object);

        var saved = await db.Notifications.SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(saved.ProviderMessageId, Is.Not.Null.And.Not.Empty);
        Assert.That(saved.SentAtUtc, Is.Not.Null);

        emailSender.Verify(x => x.SendAsync("buyer@capshop.com", "Payment received", It.IsAny<string>()), Times.Once);
    }

    [Test]
    public async Task PaymentFailedConsumer_WhenEmailMissing_UsesFallbackRecipient()
    {
        await using var db = CreateDbContext();
        var sut = new PaymentFailedEventConsumer(db);

        var msg = new TestPaymentFailedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 13,
            UserId = 77,
            UserEmail = "",
            PaymentId = 36,
            FailureReason = "Bank timeout",
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<PaymentFailedEvent>>();
        ctx.SetupGet(x => x.Message).Returns(msg);

        await sut.Consume(ctx.Object);

        var saved = await db.Notifications.SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(saved.Recipient, Is.EqualTo("user-77@example.com"));
        Assert.That(saved.Subject, Is.EqualTo("Payment failed"));
    }

    [Test]
    public async Task OrderPlacedConsumer_WhenAlreadySent_DoesNothing()
    {
        await using var db = CreateDbContext();
        db.Notifications.Add(new NotificationRecord
        {
            OrderId = 44,
            Channel = "Email",
            Subject = "Order placed",
            Recipient = "buyer@capshop.com",
            Message = "already",
            Status = NotificationStatus.Sent
        });
        await db.SaveChangesAsync();

        var emailSender = new Mock<IEmailSender>();
        var sut = new OrderPlacedEventConsumer(db, emailSender.Object);

        var msg = new TestOrderPlacedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 44,
            UserId = 8,
            UserEmail = "buyer@capshop.com",
            OrderNumber = "ORD-44",
            TotalAmount = 450,
            Items = new List<OrderPlacedItemEvent>(),
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<OrderPlacedEvent>>();
        ctx.SetupGet(x => x.Message).Returns(msg);

        await sut.Consume(ctx.Object);

        Assert.That(await db.Notifications.CountAsync(), Is.EqualTo(1));
        emailSender.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task OrderPlacedConsumer_WhenEmailPresent_SendsAndStoresNotification()
    {
        await using var db = CreateDbContext();
        var emailSender = new Mock<IEmailSender>();
        var sut = new OrderPlacedEventConsumer(db, emailSender.Object);

        var msg = new TestOrderPlacedEvent
        {
            CorrelationId = Guid.NewGuid(),
            OrderId = 45,
            UserId = 9,
            UserEmail = "buyer@capshop.com",
            OrderNumber = "ORD-45",
            TotalAmount = 900,
            Items = new List<OrderPlacedItemEvent>
            {
                new TestOrderPlacedItemEvent
                {
                    ProductId = 1,
                    Title = "Cap",
                    Description = "Blue cap",
                    Price = 450,
                    Quantity = 2,
                    Amount = 900
                }
            },
            OccurredAtUtc = DateTime.UtcNow
        };

        var ctx = new Mock<ConsumeContext<OrderPlacedEvent>>();
        ctx.SetupGet(x => x.Message).Returns(msg);

        await sut.Consume(ctx.Object);

        var saved = await db.Notifications.SingleAsync();
        Assert.That(saved.Status, Is.EqualTo(NotificationStatus.Sent));
        Assert.That(saved.Subject, Is.EqualTo("Order placed"));
        Assert.That(saved.ProviderMessageId, Is.Not.Null.And.Not.Empty);

        emailSender.Verify(x => x.SendAsync("buyer@capshop.com", "Order placed", It.IsAny<string>()), Times.Once);
    }

    private sealed class TestPaymentSucceededEvent : PaymentSucceededEvent
    {
        public Guid CorrelationId { get; init; }
        public int OrderId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public int PaymentId { get; init; }
        public string TransactionId { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public DateTime OccurredAtUtc { get; init; }
    }

    private sealed class TestPaymentFailedEvent : PaymentFailedEvent
    {
        public Guid CorrelationId { get; init; }
        public int OrderId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public int PaymentId { get; init; }
        public string FailureReason { get; init; } = string.Empty;
        public DateTime OccurredAtUtc { get; init; }
    }

    private sealed class TestOrderPlacedEvent : OrderPlacedEvent
    {
        public Guid CorrelationId { get; init; }
        public int OrderId { get; init; }
        public int UserId { get; init; }
        public string UserEmail { get; init; } = string.Empty;
        public string OrderNumber { get; init; } = string.Empty;
        public decimal TotalAmount { get; init; }
        public IEnumerable<OrderPlacedItemEvent> Items { get; init; } = new List<OrderPlacedItemEvent>();
        public DateTime OccurredAtUtc { get; init; }
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
