using CapShop.OrderService.Data;
using CapShop.OrderService.Models;
using CapShop.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CapShop.OrderService.Sagas;

public class OrderSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string? CurrentState { get; set; }
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public string UserEmail { get; set; } = string.Empty;
    public string OrderNumber { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}

public class OrderSagaStateMachine : MassTransitStateMachine<OrderSagaState>
{
    private readonly IServiceScopeFactory _scopeFactory;

    public State AwaitingInventory { get; private set; } = null!;

    public Event<PaymentSucceededEvent> PaymentSucceeded { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;
    public Event<StockReservedEvent> StockReserved { get; private set; } = null!;
    public Event<StockReservationFailedEvent> StockReservationFailed { get; private set; } = null!;

    public OrderSagaStateMachine(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;

        InstanceState(x => x.CurrentState);

        Event(() => PaymentSucceeded, x =>
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.InsertOnInitial = true;
        });

        Event(() => PaymentFailed, x =>
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.InsertOnInitial = true;
        });

        Event(() => StockReserved, x =>
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.InsertOnInitial = true;
            x.SetSagaFactory(context => new OrderSagaState
            {
                CorrelationId = context.Message.CorrelationId,
                OrderId = context.Message.OrderId,
                UserId = context.Message.UserId,
                UserEmail = string.Empty,
                OrderNumber = context.Message.OrderNumber,
                TotalAmount = context.Message.TotalAmount
            });
        });

        Event(() => StockReservationFailed, x =>
        {
            x.CorrelateById(context => context.Message.CorrelationId);
            x.InsertOnInitial = true;
            x.SetSagaFactory(context => new OrderSagaState
            {
                CorrelationId = context.Message.CorrelationId,
                OrderId = context.Message.OrderId,
                UserId = context.Message.UserId,
                UserEmail = string.Empty,
                OrderNumber = context.Message.OrderNumber,
                TotalAmount = 0m
            });
        });

        Initially(
            When(PaymentSucceeded)
                .ThenAsync(HandlePaymentSucceededAsync)
                .TransitionTo(AwaitingInventory),
            When(PaymentFailed)
                .ThenAsync(HandlePaymentFailedAsync)
                .Finalize(),
            When(StockReserved)
                .ThenAsync(HandleStockReservedAsync)
                .Finalize(),
            When(StockReservationFailed)
                .ThenAsync(HandleStockReservationFailedAsync)
                .Finalize());

        During(AwaitingInventory,
            Ignore(PaymentSucceeded),
            Ignore(PaymentFailed),
            When(StockReserved)
                .ThenAsync(HandleStockReservedAsync)
                .Finalize(),
            When(StockReservationFailed)
                .ThenAsync(HandleStockReservationFailedAsync)
                .Finalize());

        SetCompletedWhenFinalized();
    }

    private async Task HandlePaymentSucceededAsync(BehaviorContext<OrderSagaState, PaymentSucceededEvent> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);

        if (order is null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        if (order.Status != OrderStatus.Paid)
        {
            var oldStatus = order.Status;
            order.Status = OrderStatus.Paid;
            order.PaymentTransactionId = context.Message.TransactionId;
            order.UpdatedAtUtc = DateTime.UtcNow;

            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = OrderStatus.Paid,
                Notes = "Paid via PaymentService RabbitMQ event"
            });

            await db.SaveChangesAsync(context.CancellationToken);
        }

        context.Saga.OrderId = order.Id;
        context.Saga.UserId = order.UserId;
        context.Saga.UserEmail = context.Message.UserEmail;
        context.Saga.OrderNumber = order.OrderNumber;
        context.Saga.TotalAmount = order.TotalAmount;

        var reservationAlreadyStarted = await db.OrderStatusHistories.AnyAsync(h =>
            h.OrderId == order.Id &&
            h.FromStatus == OrderStatus.Paid &&
            h.ToStatus == OrderStatus.Paid &&
            (h.Notes == "Reserve stock started by place-order fallback" ||
             h.Notes == "Reserve stock command published by saga"),
            context.CancellationToken);

        if (reservationAlreadyStarted)
        {
            return;
        }

        await context.Publish<ReserveStockCommand>(new
        {
            context.Message.CorrelationId,
            OrderId = order.Id,
            UserId = order.UserId,
            UserEmail = context.Message.UserEmail,
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

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = OrderStatus.Paid,
            ToStatus = OrderStatus.Paid,
            Notes = "Reserve stock command published by saga"
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private async Task HandlePaymentFailedAsync(BehaviorContext<OrderSagaState, PaymentFailedEvent> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);
        if (order is null || order.Status == OrderStatus.Cancelled || order.Status == OrderStatus.Completed)
        {
            return;
        }

        var oldStatus = order.Status;
        order.Status = OrderStatus.Cancelled;
        order.UpdatedAtUtc = DateTime.UtcNow;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = oldStatus,
            ToStatus = OrderStatus.Cancelled,
            Notes = $"Payment failed: {context.Message.FailureReason}"
        });

        await db.SaveChangesAsync(context.CancellationToken);
    }

    private async Task HandleStockReservedAsync(BehaviorContext<OrderSagaState, StockReservedEvent> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);

        if (order is null || order.Status == OrderStatus.Completed || order.Status == OrderStatus.Cancelled)
        {
            return;
        }

        var oldStatus = order.Status;
        order.Status = OrderStatus.Paid;
        order.UpdatedAtUtc = DateTime.UtcNow;

        db.OrderStatusHistories.Add(new OrderStatusHistory
        {
            OrderId = order.Id,
            FromStatus = oldStatus,
            ToStatus = OrderStatus.Paid,
            Notes = "Inventory reserved. Order remains paid."
        });

        var cart = await db.Carts
            .Include(x => x.Items)
            .FirstOrDefaultAsync(x => x.UserId == order.UserId, context.CancellationToken);
        if (cart is not null)
        {
            cart.Items.Clear();
            cart.UpdatedAtUtc = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(context.CancellationToken);

        await context.Publish<OrderPlacedEvent>(new
        {
            context.Message.CorrelationId,
            OrderId = order.Id,
            UserId = order.UserId,
            UserEmail = context.Saga.UserEmail,
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

    private async Task HandleStockReservationFailedAsync(BehaviorContext<OrderSagaState, StockReservationFailedEvent> context)
    {
        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var order = await db.Orders.FirstOrDefaultAsync(x => x.Id == context.Message.OrderId, context.CancellationToken);
        if (order is null || order.Status == OrderStatus.Completed)
        {
            return;
        }

        var oldStatus = order.Status;
        if (order.Status != OrderStatus.Cancelled)
        {
            order.Status = OrderStatus.Cancelled;
            order.UpdatedAtUtc = DateTime.UtcNow;

            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = oldStatus,
                ToStatus = OrderStatus.Cancelled,
                Notes = $"Stock reservation failed: {context.Message.FailureReason}"
            });
        }

        var refundAlreadyRequested = await db.OrderStatusHistories.AnyAsync(h =>
            h.OrderId == order.Id &&
            h.FromStatus == OrderStatus.Cancelled &&
            h.ToStatus == OrderStatus.Cancelled &&
            h.Notes == "Refund payment command published by saga",
            context.CancellationToken);

        if (!refundAlreadyRequested)
        {
            await context.Publish<RefundPaymentCommand>(new
            {
                context.Message.CorrelationId,
                OrderId = order.Id,
                UserId = order.UserId,
                UserEmail = context.Saga.UserEmail,
                OrderNumber = order.OrderNumber,
                Amount = order.TotalAmount,
                TransactionId = order.PaymentTransactionId,
                Reason = $"Stock reservation failed: {context.Message.FailureReason}",
                OccurredAtUtc = DateTime.UtcNow
            });

            db.OrderStatusHistories.Add(new OrderStatusHistory
            {
                OrderId = order.Id,
                FromStatus = OrderStatus.Cancelled,
                ToStatus = OrderStatus.Cancelled,
                Notes = "Refund payment command published by saga"
            });
        }

        await db.SaveChangesAsync(context.CancellationToken);
    }
}
