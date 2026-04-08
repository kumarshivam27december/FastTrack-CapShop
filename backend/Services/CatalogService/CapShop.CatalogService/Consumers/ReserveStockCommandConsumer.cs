using CapShop.CatalogService.Infrastructure.Repositories;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.CatalogService.Consumers;

public class ReserveStockCommandConsumer : IConsumer<ReserveStockCommand>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly IPublishEndpoint _publishEndpoint;

    public ReserveStockCommandConsumer(ICatalogRepository catalogRepository, IPublishEndpoint publishEndpoint)
    {
        _catalogRepository = catalogRepository;
        _publishEndpoint = publishEndpoint;
    }

    public async Task Consume(ConsumeContext<ReserveStockCommand> context)
    {
        var msg = context.Message;
        var items = msg.Items?.ToList() ?? new List<OrderPlacedItemEvent>();

        var reserved = await _catalogRepository.ReserveStockAsync(items, context.CancellationToken);
        if (!reserved)
        {
            await _publishEndpoint.Publish<StockReservationFailedEvent>(new
            {
                msg.CorrelationId,
                msg.OrderId,
                msg.UserId,
                msg.OrderNumber,
                FailureReason = "Insufficient stock for one or more products.",
                OccurredAtUtc = DateTime.UtcNow
            });
            return;
        }

        await _publishEndpoint.Publish<StockReservedEvent>(new
        {
            msg.CorrelationId,
            msg.OrderId,
            msg.UserId,
            msg.OrderNumber,
            msg.TotalAmount,
            OccurredAtUtc = DateTime.UtcNow
        });
    }
}