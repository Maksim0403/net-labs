using System.Data;

namespace Shipping.Dal.Abstractions;

/// <summary>
/// Одне з'єднання і одна транзакція на всю бізнес-операцію (Scoped у DI —
/// один екземпляр на HTTP-запит). Усі репозитори отримують це саме з'єднання
/// і поточну транзакцію (Крок B.4).
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository Customers { get; }
    IShipmentRepository Shipments { get; }
    IServiceTypeRepository ServiceTypes { get; }

    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
