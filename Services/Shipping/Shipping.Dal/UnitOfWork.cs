using System.Data;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;
using Shipping.Dal.Repositories;

namespace Shipping.Dal;

/// <summary>Unit of Work з одним з'єднанням і транзакцією (ReadCommitted).</summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);

        Customers = new CustomerRepository(_connection, () => _transaction);
        Shipments = new ShipmentRepository(_connection, () => _transaction);
        ServiceTypes = new ServiceTypeRepository(_connection, () => _transaction);
    }

    public ICustomerRepository Customers { get; }
    public IShipmentRepository Shipments { get; }
    public IServiceTypeRepository ServiceTypes { get; }

    public async Task BeginTransactionAsync(
        IsolationLevel level = IsolationLevel.ReadCommitted, CancellationToken ct = default)
    {
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(level, ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.CommitAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_transaction is null) return;
        await _transaction.RollbackAsync(ct);
        await _transaction.DisposeAsync();
        _transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
            await _transaction.DisposeAsync();
        await _connection.DisposeAsync();
    }
}

