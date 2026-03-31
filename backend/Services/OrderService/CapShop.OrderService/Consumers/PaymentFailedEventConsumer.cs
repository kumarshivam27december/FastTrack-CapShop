using CapShop.OrderService.Data;
using CapShop.OrderService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CapShop.OrderService.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly OrderDbContext _db;

    public PaymentFailedEventConsumer(OrderDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var msg = context.Message;
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == msg.OrderId);
        if (order is null)
        {
            return;
        }

        if (order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        var oldStatus = order.Status;
        order.Status = OrderStatus.PaymentPending;
        order.UpdatedAtUtc = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = oldStatus,
            ToStatus = OrderStatus.PaymentPending,
            Notes = $"Payment failed: {msg.FailureReason}"
        });

        await _db.SaveChangesAsync();
    }
}
