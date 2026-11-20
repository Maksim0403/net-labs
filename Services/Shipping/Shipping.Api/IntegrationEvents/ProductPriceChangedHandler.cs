using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Shipping.Dal.Abstractions;

namespace Shipping.Api.IntegrationEvents;

/// <summary>
/// Сервіс замовлень (Лаб. 02): оновлює локальну копію довідника Products (політика з Лаб. 01).
/// Ідемпотентна обробка події зміни ціни товару.
/// </summary>
public class ProductPriceChangedHandler(IUnitOfWork uow, ILogger<ProductPriceChangedHandler> logger)
{
    public async Task HandleAsync(ProductPriceChangedEvent e, CancellationToken ct)
    {
        logger.LogInformation(
            "Processing ProductPriceChangedEvent {EventId} for ProductId {ProductId}, ProductName {ProductName}, NewPrice {NewPrice}, CorrelationId {CorrelationId}",
            e.EventId, e.ProductId, e.ProductName, e.NewPrice, e.CorrelationId);

        await uow.BeginTransactionAsync(ct: ct);
        try
        {
            if (!await uow.ProcessedEvents.TryAddAsync(e.EventId, ct))        // уже оброблено
            {
                logger.LogInformation("Duplicate event {EventId} for ProductId {ProductId} skipped", e.EventId, e.ProductId);
                await uow.RollbackAsync(ct);
                return;
            }
            await uow.Products.UpdateSnapshotAsync(e.ProductId, e.ProductName, e.NewPrice, ct);
            await uow.CommitAsync(ct);

            logger.LogInformation(
                "Successfully applied ProductPriceChangedEvent {EventId} for ProductId {ProductId}",
                e.EventId, e.ProductId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Failed to process ProductPriceChangedEvent {EventId} for ProductId {ProductId}, rolling back transaction",
                e.EventId, e.ProductId);
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
