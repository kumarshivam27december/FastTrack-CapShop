using CapShop.PaymentService.Data;
using CapShop.PaymentService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CapShop.PaymentService.Consumers;

public class RefundPaymentCommandConsumer : IConsumer<RefundPaymentCommand>
{
    private readonly PaymentDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public RefundPaymentCommandConsumer(PaymentDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
    {
        var msg = context.Message;

        var payment = await _db.Payments
            .OrderByDescending(x => x.CreatedAtUtc)
            .FirstOrDefaultAsync(x => x.OrderId == msg.OrderId, context.CancellationToken);

        if (payment is null)
        {
            await PublishRefundFailedAsync(msg, "No payment record found for order.", context.CancellationToken);
            return;
        }

        if (payment.Status == PaymentStatus.Refunded)
        {
            return;
        }

        if (payment.Status != PaymentStatus.Succeeded)
        {
            await PublishRefundFailedAsync(msg, $"Payment status '{payment.Status}' is not refundable.", context.CancellationToken);
            return;
        }

        payment.Status = PaymentStatus.Refunded;
        payment.FailureReason = null;
        payment.UpdatedAtUtc = DateTime.UtcNow;

        await _db.SaveChangesAsync(context.CancellationToken);

        await _publishEndpoint.Publish<PaymentRefundedEvent>(new
        {
            msg.CorrelationId,
            msg.OrderId,
            msg.UserId,
            msg.UserEmail,
            PaymentId = payment.Id,
            Amount = payment.Amount,
            TransactionId = payment.TransactionId,
            OccurredAtUtc = DateTime.UtcNow
        }, context.CancellationToken);
    }

    private Task PublishRefundFailedAsync(RefundPaymentCommand msg, string reason, CancellationToken cancellationToken)
    {
        return _publishEndpoint.Publish<PaymentRefundFailedEvent>(new
        {
            msg.CorrelationId,
            msg.OrderId,
            msg.UserId,
            msg.UserEmail,
            FailureReason = reason,
            OccurredAtUtc = DateTime.UtcNow
        }, cancellationToken);
    }
}
