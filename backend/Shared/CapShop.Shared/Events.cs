namespace CapShop.Shared.Events;

/// <summary>
/// Published by OrderService when a new order is created and ready for payment.
/// </summary>
public interface OrderCreatedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    decimal TotalAmount { get; }
    string OrderNumber { get; }
    string PaymentMethod { get; }
    bool SimulateSuccess { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by PaymentService when payment is successfully processed.
/// </summary>
public interface PaymentSucceededEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    int PaymentId { get; }
    string TransactionId { get; }
    decimal Amount { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by PaymentService when payment processing fails.
/// </summary>
public interface PaymentFailedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    int PaymentId { get; }
    string FailureReason { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by OrderService when an order is successfully placed (after payment succeeded).
/// </summary>
public interface OrderPlacedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    string OrderNumber { get; }
    decimal TotalAmount { get; }
    IEnumerable<OrderPlacedItemEvent> Items { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by OrderService to request stock reservation after payment succeeds.
/// </summary>
public interface ReserveStockCommand
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    string OrderNumber { get; }
    decimal TotalAmount { get; }
    IEnumerable<OrderPlacedItemEvent> Items { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by CatalogService when stock reservation succeeds.
/// </summary>
public interface StockReservedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string OrderNumber { get; }
    decimal TotalAmount { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by CatalogService when stock reservation fails.
/// </summary>
public interface StockReservationFailedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string OrderNumber { get; }
    string FailureReason { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by OrderService when stock reservation fails after a successful payment.
/// </summary>
public interface RefundPaymentCommand
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    string OrderNumber { get; }
    decimal Amount { get; }
    string? TransactionId { get; }
    string Reason { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by PaymentService when a refund is successfully processed.
/// </summary>
public interface PaymentRefundedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    int PaymentId { get; }
    decimal Amount { get; }
    string? TransactionId { get; }
    DateTime OccurredAtUtc { get; }
}

/// <summary>
/// Published by PaymentService when a refund request cannot be processed.
/// </summary>
public interface PaymentRefundFailedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string UserEmail { get; }
    string FailureReason { get; }
    DateTime OccurredAtUtc { get; }
}

public interface OrderPlacedItemEvent
{
    int ProductId { get; }
    string Title { get; }
    string Description { get; }
    decimal Price { get; }
    int Quantity { get; }
    decimal Amount { get; }
}

/// <summary>
/// Published by OrderService when order status changes.
/// </summary>
public interface OrderStatusChangedEvent
{
    Guid CorrelationId { get; }
    int OrderId { get; }
    int UserId { get; }
    string OldStatus { get; }
    string NewStatus { get; }
    string? Notes { get; }
    DateTime OccurredAtUtc { get; }
}
