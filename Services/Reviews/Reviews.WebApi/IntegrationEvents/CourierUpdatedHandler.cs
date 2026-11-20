using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Reviews.Domain.Repositories;

namespace Reviews.WebApi.IntegrationEvents;

/// <summary>
/// Обробник події оновлення кур'єра для синхронізації денормалізованого імені в колекції відгуків.
/// </summary>
public class CourierUpdatedHandler(
    IReviewRepository reviewRepository,
    IProcessedEventRepository processedEventRepository,
    ILogger<CourierUpdatedHandler> logger)
{
    public async Task HandleAsync(CourierUpdatedEvent e, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing CourierUpdatedEvent {EventId} for CourierId {CourierId}, FullName {FullName}, CorrelationId {CorrelationId}",
            e.EventId, e.CourierId, e.FullName, e.CorrelationId);

        if (!await processedEventRepository.TryAddAsync(e.EventId, ct))
        {
            logger.LogInformation("Duplicate event {EventId} for CourierId {CourierId} skipped", e.EventId, e.CourierId);
            return;
        }

        try
        {
            await reviewRepository.UpdateCourierNameAsync(e.CourierId, e.FullName, ct);
            logger.LogInformation("Updated reviews for courier {CourierId} with new name {FullName}, EventId: {EventId}",
                e.CourierId, e.FullName, e.EventId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to update reviews for courier {CourierId} with new name {FullName}, EventId: {EventId}",
                e.CourierId, e.FullName, e.EventId);
            throw;
        }
    }
}
