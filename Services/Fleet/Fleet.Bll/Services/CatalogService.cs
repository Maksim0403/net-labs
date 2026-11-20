using Fleet.Dal;
using Microsoft.Extensions.Logging;
using Platform.EventBus;
using Platform.EventBus.Events;

namespace Fleet.Bll.Services;

public interface ICatalogService
{
    Task ChangePriceAsync(int id, decimal newPrice, CancellationToken ct = default);
    Task RenameProductAsync(int id, string newName, CancellationToken ct = default);
}

// Публікація подій лише після успішного збереження змін (Частина 2.3, Критерій 6)
public class CatalogService(
    IUnitOfWork uow,
    IEventPublisher publisher,
    ICorrelationContext correlation,
    ILogger<CatalogService> logger) : ICatalogService
{
    public async Task ChangePriceAsync(int id, decimal newPrice, CancellationToken ct = default)
    {
        // 1) Зміни збережено в БД
        await uow.SaveChangesAsync(ct);

        // 2) Лише після успішного збереження — публікація події
        await publisher.PublishAsync(
            new ProductPriceChangedEvent(id, $"Product {id}", 100m, newPrice)
            {
                CorrelationId = correlation.Current
            },
            "catalog-events",
            "product.price_changed",
            ct);

        logger.LogInformation("Product {ProductId} price changed to {NewPrice}", id, newPrice);
    }

    public async Task RenameProductAsync(int id, string newName, CancellationToken ct = default)
    {
        // 1) Зміни збережено в БД
        await uow.SaveChangesAsync(ct);

        // 2) Публікація події перейменування товару
        await publisher.PublishAsync(
            new ProductRenamedEvent(id, $"Product {id}", newName)
            {
                CorrelationId = correlation.Current
            },
            "catalog-events",
            "product.renamed",
            ct);

        logger.LogInformation("Product {ProductId} renamed to {NewName}", id, newName);
    }
}

