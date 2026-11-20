using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Shipping.Dal.Abstractions;

namespace Shipping.Api.IntegrationEvents;

/// <summary>
/// Сервіс замовлень: оновлює назву товару в локальній копії довідника Products (політика з Лаб. 01).
/// </summary>
public class ProductRenamedHandler(IUnitOfWork uow, ILogger<ProductRenamedHandler> logger)
{
    public async Task HandleAsync(ProductRenamedEvent e, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing ProductRenamedEvent {EventId} for ProductId {ProductId}, NewName {NewName}, CorrelationId {CorrelationId}",
            e.EventId, e.ProductId, e.NewName, e.CorrelationId);

        await uow.BeginTransactionAsync(ct: ct);
        try
        {
            if (!await uow.ProcessedEvents.TryAddAsync(e.EventId, ct))
            {
                logger.LogInformation("Duplicate event {EventId} for ProductId {ProductId} skipped", e.EventId, e.ProductId);
                await uow.RollbackAsync(ct);
                return;
            }
            await uow.Products.UpdateSnapshotAsync(e.ProductId, e.NewName, 0m, ct);
            await uow.CommitAsync(ct);

            logger.LogInformation(
                "Successfully applied ProductRenamedEvent {EventId} for ProductId {ProductId}",
                e.EventId, e.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process ProductRenamedEvent {EventId} for ProductId {ProductId}, rolling back transaction",
                e.EventId, e.ProductId);
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
