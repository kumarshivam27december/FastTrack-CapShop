using CapShop.NotificationService.Data;
using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.NotificationService.Consumers;

public class PaymentSucceededEventConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly NotificationDbContext _db;
    private readonly IEmailSender _emailSender;

    public PaymentSucceededEventConsumer(NotificationDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var msg = context.Message;
        var recipient = msg.UserEmail?.Trim() ?? string.Empty;
        var subject = "Payment received";
        var body = $"Payment received for order #{msg.OrderId}. Transaction: {msg.TransactionId}";

        var record = new NotificationRecord
        {
            UserId = msg.UserId,
            OrderId = msg.OrderId,
            PaymentId = msg.PaymentId,
            Channel = "Email",
            Recipient = recipient,
            Subject = subject,
            Message = body,
            Status = NotificationStatus.Failed
        };

        if (string.IsNullOrWhiteSpace(recipient))
        {
            record.ErrorMessage = "User email is missing in event payload.";
            _db.Notifications.Add(record);
            await _db.SaveChangesAsync();
            return;
        }

        try
        {
            await _emailSender.SendAsync(recipient, subject, body);
            record.Status = NotificationStatus.Sent;
            record.ProviderMessageId = $"MSG-{Guid.NewGuid():N}";
            record.SentAtUtc = DateTime.UtcNow;
            record.ErrorMessage = null;
        }
        catch (Exception ex)
        {
            record.Status = NotificationStatus.Failed;
            record.ErrorMessage = ex.Message;
        }

        _db.Notifications.Add(record);

        await _db.SaveChangesAsync();
    }
}
