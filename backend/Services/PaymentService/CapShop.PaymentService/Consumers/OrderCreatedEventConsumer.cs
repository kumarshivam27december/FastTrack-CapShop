using CapShop.PaymentService.Data;
using CapShop.PaymentService.Models;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.PaymentService.Consumers;

public class OrderCreatedEventConsumer : IConsumer<OrderCreatedEvent>
{
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderCreatedEventConsumer(PaymentDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<OrderCreatedEvent> context)
    {
        var msg = context.Message;

        var payment = new PaymentRecord
        {
            OrderId = msg.OrderId,
            UserId = msg.UserId,
            Amount = msg.TotalAmount,
            Currency = "INR",
            PaymentMethod = msg.PaymentMethod,
            Status = msg.SimulateSuccess ? PaymentStatus.Succeeded : PaymentStatus.Failed,
            TransactionId = msg.SimulateSuccess ? $"TXN-{Guid.NewGuid():N}" : null,
            FailureReason = msg.SimulateSuccess ? null : "Simulated payment failure"
        };

        _db.Payments.Add(payment);
        await _db.SaveChangesAsync();

        if (msg.SimulateSuccess)
        {
            await _publishEndpoint.Publish<PaymentSucceededEvent>(new
            {
                msg.CorrelationId,
                msg.OrderId,
                msg.UserId,
                msg.UserEmail,
                PaymentId = payment.Id,
                TransactionId = payment.TransactionId ?? string.Empty,
                Amount = payment.Amount,
                OccurredAtUtc = DateTime.UtcNow
            });
            return;
        }

        await _publishEndpoint.Publish<PaymentFailedEvent>(new
        {
            msg.CorrelationId,
            msg.OrderId,
            msg.UserId,
            msg.UserEmail,
            PaymentId = payment.Id,
            FailureReason = payment.FailureReason ?? "Payment failed",
            OccurredAtUtc = DateTime.UtcNow
        });
    }
}
