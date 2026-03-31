using CapShop.NotificationService.Data;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.NotificationService.Consumers;

public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly NotificationDbContext _db;

    public OrderPlacedEventConsumer(NotificationDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var msg = context.Message;

        _db.Notifications.Add(new NotificationRecord
        {
            UserId = msg.UserId,
            OrderId = msg.OrderId,
            Channel = "Email",
            Recipient = string.IsNullOrWhiteSpace(msg.UserEmail) ? $"user-{msg.UserId}@example.com" : msg.UserEmail,
            Subject = "Order placed",
            Message = $"Your order {msg.OrderNumber} has been placed successfully.",
            Status = NotificationStatus.Sent,
            ProviderMessageId = $"MSG-{Guid.NewGuid():N}",
            SentAtUtc = DateTime.UtcNow
        });

        await _db.SaveChangesAsync();
    }
}
