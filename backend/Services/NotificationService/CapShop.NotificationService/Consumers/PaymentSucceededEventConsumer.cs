using CapShop.NotificationService.Data;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.NotificationService.Consumers;

public class PaymentSucceededEventConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly NotificationDbContext _db;

    public PaymentSucceededEventConsumer(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var msg = context.Message;

        _db.Notifications.Add(new NotificationRecord
        {
            UserId = msg.UserId,
            OrderId = msg.OrderId,
            PaymentId = msg.PaymentId,
            Channel = "Email",
            Recipient = string.IsNullOrWhiteSpace(msg.UserEmail) ? $"user-{msg.UserId}@example.com" : msg.UserEmail,
            Subject = "Payment received",
            Message = $"Payment received for order #{msg.OrderId}. Transaction: {msg.TransactionId}",
            Status = NotificationStatus.Sent,
            ProviderMessageId = $"MSG-{Guid.NewGuid():N}",
            SentAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
