using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Shipping.Dal.Abstractions;

namespace Shipping.Api.IntegrationEvents;

/// <summary>
/// Сервіс доставки (Orders / Shipping): ідемпотентно оновлює денормалізований знімок кур'єра в замовленнях.
/// </summary>
public class CourierUpdatedHandler(IUnitOfWork uow, ILogger<CourierUpdatedHandler> logger)
{
    public async Task HandleAsync(CourierUpdatedEvent e, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing CourierUpdatedEvent {EventId} for CourierId {CourierId}, FullName {FullName}, CorrelationId {CorrelationId}",
            e.EventId, e.CourierId, e.FullName, e.CorrelationId);

        await uow.BeginTransactionAsync(ct: ct);
        try
        {
            if (!await uow.ProcessedEvents.TryAddAsync(e.EventId, ct))
            {
                logger.LogInformation("Duplicate event {EventId} for CourierId {CourierId} skipped", e.EventId, e.CourierId);
                await uow.RollbackAsync(ct);
                return;
            }
            await uow.Shipments.UpdateCourierSnapshotAsync(e.CourierId, e.FullName, e.Phone, ct);
            await uow.CommitAsync(ct);

            logger.LogInformation(
                "Successfully applied CourierUpdatedEvent {EventId} for CourierId {CourierId}",
                e.EventId, e.CourierId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process CourierUpdatedEvent {EventId} for CourierId {CourierId}, rolling back transaction",
                e.EventId, e.CourierId);
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
