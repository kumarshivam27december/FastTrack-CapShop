using CapShop.ReviewService.Application.Interfaces;
using CapShop.Shared.Events;
using MassTransit;

namespace CapShop.ReviewService.Consumers;

public class OrderReviewEligibilityCreatedEventConsumer : IConsumer<OrderReviewEligibilityCreatedEvent>
{
    private readonly IReviewAppService _reviewService;
    private readonly ILogger<OrderReviewEligibilityCreatedEventConsumer> _logger;

    public OrderReviewEligibilityCreatedEventConsumer(IReviewAppService reviewService, ILogger<OrderReviewEligibilityCreatedEventConsumer> logger)
    {
        _reviewService = reviewService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderReviewEligibilityCreatedEvent> context)
    {
        foreach (var item in context.Message.Items)
        {
            await _reviewService.UpsertEligibilityAsync(
                context.Message.UserId,
                item.ProductId,
                context.Message.OrderId,
                item.ProductName,
                context.Message.DeliveredAtUtc,
                context.CancellationToken);
        }

        _logger.LogInformation("Review eligibility synced for order {OrderId}", context.Message.OrderId);
    }
}
