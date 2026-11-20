using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;

namespace Shipping.Dal.Repositories;

/// <summary>Реалізація перевірки ідемпотентності через таблицю dbo.ProcessedEvents.</summary>
public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly SqlConnection _connection;
    private readonly Func<SqlTransaction?> _tx;

    public ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> tx)
    {
        _connection = connection;
        _tx = tx;
    }

    public async Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT INTO dbo.ProcessedEvents (EventId, ProcessedAt)
            VALUES (@EventId, SYSUTCDATETIME());";

        try
        {
            var command = new CommandDefinition(sql, new { EventId = eventId }, transaction: _tx(), cancellationToken: ct);
            await _connection.ExecuteAsync(command);
            return true;
        }
        catch (SqlException ex) when (ex.Number is 2627 or 2601) // Primary key / Unique violation
        {
            return false;
        }
    }
}

