using CapShop.NotificationService.Data;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.NotificationService.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly NotificationDbContext _db;

    public PaymentFailedEventConsumer(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var msg = context.Message;

        _db.Notifications.Add(new NotificationRecord
        {
            UserId = msg.UserId,
            OrderId = msg.OrderId,
            PaymentId = msg.PaymentId,
            Channel = "Email",
            Recipient = string.IsNullOrWhiteSpace(msg.UserEmail) ? $"user-{msg.UserId}@example.com" : msg.UserEmail,
            Subject = "Payment failed",
            Message = $"Payment failed for order #{msg.OrderId}. Reason: {msg.FailureReason}",
            Status = NotificationStatus.Sent,
            ProviderMessageId = $"MSG-{Guid.NewGuid():N}",
            SentAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
