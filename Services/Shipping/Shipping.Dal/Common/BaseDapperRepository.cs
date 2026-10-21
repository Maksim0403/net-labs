using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Shipping.Dal.Common;

/// <summary>
/// Спільний CRUD на Dapper + ADO.NET для сутностей, що мапляться на одну
/// таблицю з первинним ключем Id (BIGINT IDENTITY). Ім'я таблиці — константа
/// з коду конкретного репозиторію, НЕ вхідні дані користувача, тому підстановка
/// в текст запиту безпечна (Крок B.3). Конкретні репозиторії додають лише
/// специфічні запити (JOIN, multi-mapping, виклики процедур).
/// </summary>
public abstract class BaseDapperRepository<T> where T : class
{
    protected readonly SqlConnection Connection;
    private readonly Func<SqlTransaction?> _currentTransaction;
    private readonly string _table;
    private readonly string _idColumn;

    protected SqlTransaction? Transaction => _currentTransaction();

    protected BaseDapperRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction,
                                    string table, string idColumn = "Id")
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
        _table = table;
        _idColumn = idColumn;
    }

    protected async Task EnsureOpenAsync(CancellationToken ct)
    {
        if (Connection.State != ConnectionState.Open)
            await Connection.OpenAsync(ct);
    }

    public virtual async Task<T?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {_table} WHERE {_idColumn} = @Id AND IsDeleted = 0";
        return await Connection.QuerySingleOrDefaultAsync<T>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"SELECT * FROM {_table} WHERE IsDeleted = 0";
        var rows = await Connection.QueryAsync<T>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>
    /// Створення залежить від схеми конкретної сутності та реалізується
    /// спеціалізованим репозиторієм параметризованим SQL або процедурою.
    /// </summary>
    public abstract Task<long> AddAsync(T entity, CancellationToken ct = default);

    public virtual async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var sql = $"UPDATE {_table} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME() WHERE {_idColumn} = @Id";
        var affected = await Connection.ExecuteAsync(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
        return affected > 0;
    }
}
