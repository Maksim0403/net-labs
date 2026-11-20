using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Reviews.Domain.Repositories;

namespace Reviews.WebApi.IntegrationEvents;

/// <summary>
/// Обробник події зміни назви товару (Лаб. 08): оновлює денормалізовану назву в усіх відгуках про товар.
/// </summary>
public class ProductRenamedHandler(
    IReviewRepository reviewRepository,
    IProcessedEventRepository processedEventRepository,
    ILogger<ProductRenamedHandler> logger)
{
    public async Task HandleAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing ProductRenamedEvent {EventId} for ProductId {ProductId}, NewName {NewName}, CorrelationId {CorrelationId}",
            e.EventId, e.ProductId, e.NewName, e.CorrelationId);

        if (!await processedEventRepository.TryAddAsync(e.EventId, ct))
        {
            logger.LogInformation("Duplicate event {EventId} for ProductId {ProductId} skipped", e.EventId, e.ProductId);
            return;
        }

        try
        {
            await reviewRepository.UpdateProductNameAsync(e.ProductId, e.NewName, ct);
            logger.LogInformation("Updated reviews for product {ProductId} with new name {NewName}, EventId: {EventId}",
                e.ProductId, e.NewName, e.EventId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to update reviews for product {ProductId} with new name {NewName}, EventId: {EventId}",
                e.ProductId, e.NewName, e.EventId);
            throw;
        }
    }
}
