using System.Data;
using Microsoft.Data.SqlClient;

namespace Shipping.Dal.Common;

/// <summary>
/// Базовий клас для репозиторію на чистому ADO.NET: спільне з'єднання і доступ
/// до поточної транзакції Unit of Work (Крок B.2). Свідомо НЕ успадковує
/// жодного Generic Repository — демонструє роботу з SqlConnection/SqlCommand
/// без сторонніх бібліотек.
/// </summary>
public abstract class RepositoryBase
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }
}
