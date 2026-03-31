using CapShop.OrderService.Data;
using CapShop.OrderService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CapShop.OrderService.Consumers;

public class PaymentSucceededEventConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly OrderDbContext _db;

    public PaymentSucceededEventConsumer(OrderDbContext db)
    {
        _db = db;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var msg = context.Message;
        var order = await _db.Orders.FirstOrDefaultAsync(x => x.Id == msg.OrderId);
        if (order is null)
        {
            return;
        }

        if (order.Status == OrderStatus.Paid)
        {
            return;
        }

        var oldStatus = order.Status;
        order.Status = OrderStatus.Paid;
        order.PaymentTransactionId = msg.TransactionId;
        order.UpdatedAtUtc = DateTime.UtcNow;

        _db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = oldStatus,
            ToStatus = OrderStatus.Paid,
            Notes = "Paid via PaymentService RabbitMQ event"
        });

        await _db.SaveChangesAsync();
    }
}
