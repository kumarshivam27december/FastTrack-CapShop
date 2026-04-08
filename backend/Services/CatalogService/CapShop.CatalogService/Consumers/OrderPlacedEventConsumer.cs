using CapShop.CatalogService.Data;
using CapShop.CatalogService.Infrastructure.Repositories;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.CatalogService.Consumers;

public class OrderPlacedEventConsumer : IConsumer<OrderPlacedEvent>
{
    private readonly ICatalogRepository _catalogRepository;
    private readonly CatalogDbContext _db;

    public OrderPlacedEventConsumer(ICatalogRepository catalogRepository, CatalogDbContext db)
    {
        _catalogRepository = catalogRepository;
        _db = db;
    }

    public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
    {
        var msg = context.Message;
        var items = msg.Items?.ToList() ?? new List<OrderPlacedItemEvent>();
        if (!items.Any())
        {
            return;
        }

        await using var tx = await _db.Database.BeginTransactionAsync(context.CancellationToken);

        foreach (var item in items)
        {
            var decreased = await _catalogRepository.DecreaseStockAsync(item.ProductId, item.Quantity, context.CancellationToken);
            if (!decreased)
            {
                throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}.");
            }
        }

        await tx.CommitAsync(context.CancellationToken);
    }
}