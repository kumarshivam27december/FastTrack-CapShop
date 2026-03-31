using CapShop.NotificationService.Data;
using CapShop.NotificationService.Application.Interfaces;
using CapShop.NotificationService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace CapShop.NotificationService.Consumers;

public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly NotificationDbContext _db;
    private readonly IEmailSender _emailSender;

    public OrderPlacedEventConsumer(NotificationDbContext db, IEmailSender emailSender)
    {
        _db = db;
        _emailSender = emailSender;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var msg = context.Message;

        var alreadySent = await _db.Notifications.AnyAsync(n =>
            n.OrderId == msg.OrderId &&
            n.Channel == "Email" &&
            n.Subject == "Order placed");
        if (alreadySent)
        {
            return;
        }

        var recipient = msg.UserEmail?.Trim() ?? string.Empty;
        var subject = "Order placed";
        var bodyBuilder = new StringBuilder();
        bodyBuilder.AppendLine($"Your order {msg.OrderNumber} has been placed successfully.");
        bodyBuilder.AppendLine();
        bodyBuilder.AppendLine("Order details:");

        var items = msg.Items?.ToList() ?? new List<OrderPlacedItemEvent>();
        if (items.Count == 0)
        {
            bodyBuilder.AppendLine("- No item details available.");
        }
        else
        {
            for (var i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bodyBuilder.AppendLine($"{i + 1}. Title: {item.Title}");
                bodyBuilder.AppendLine($"   Description: {item.Description}");
                bodyBuilder.AppendLine($"   Price: Rs. {item.Price:F2}");
                bodyBuilder.AppendLine($"   Quantity: {item.Quantity}");
                bodyBuilder.AppendLine($"   Amount: Rs. {item.Amount:F2}");
            }
        }

        bodyBuilder.AppendLine();
        bodyBuilder.AppendLine($"Total Amount: Rs. {msg.TotalAmount:F2}");

        var body = bodyBuilder.ToString();

        var record = new NotificationRecord
        {
            UserId = msg.UserId,
            OrderId = msg.OrderId,
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
