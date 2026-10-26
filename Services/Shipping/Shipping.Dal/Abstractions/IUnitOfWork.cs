using System.Data;

namespace Shipping.Dal.Abstractions;

/// <summary>Контракт Unit of Work з одним з'єднанням і транзакцією.</summary>
public interface IUnitOfWork : IAsyncDisposable
{
    ICustomerRepository Customers { get; }
    IShipmentRepository Shipments { get; }
    IServiceTypeRepository ServiceTypes { get; }

    Task BeginTransactionAsync(IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}

