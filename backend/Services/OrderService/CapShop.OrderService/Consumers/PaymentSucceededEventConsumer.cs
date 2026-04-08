using CapShop.OrderService.Data;
using CapShop.OrderService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace CapShop.OrderService.Consumers;

public class PaymentSucceededEventConsumer : IConsumer<PaymentSucceededEvent>
{
    private readonly OrderDbContext _db;
    private readonly IPublishEndpoint _publishEndpoint;

    public PaymentSucceededEventConsumer(OrderDbContext db, IPublishEndpoint publishEndpoint)
    {
        _db = db;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<PaymentSucceededEvent> context)
    {
        var msg = context.Message;
        var order = await _db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == msg.OrderId);
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

        await _publishEndpoint.Publish<OrderPlacedEvent>(new
        {
            CorrelationId = msg.CorrelationId,
            OrderId = order.Id,
            UserId = order.UserId,
            UserEmail = msg.UserEmail,
            OrderNumber = order.OrderNumber,
            TotalAmount = order.TotalAmount,
            Items = order.Items.Select(i => new
            {
                ProductId = i.ProductId,
                Title = i.ProductName,
                Description = $"Product ID: {i.ProductId}",
                Price = i.UnitPrice,
                Quantity = i.Quantity,
                Amount = i.TotalPrice
            }).ToList(),
            OccurredAtUtc = DateTime.UtcNow
        });
    }
}
