using Dapper;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;
using Shipping.Dal.Common;
using Shipping.Domain.Models;

namespace Shipping.Dal.Repositories;

/// <summary>Один із двох репозиторіїв на Dapper (Крок B.2). Спільний CRUD — у BaseDapperRepository.</summary>
public sealed class ServiceTypeRepository : BaseDapperRepository<ServiceType>, IServiceTypeRepository
{
    public ServiceTypeRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.ServiceTypes", "ServiceTypeId") { }

    // Перевизначено, бо базовий SELECT * не підставляє alias ServiceTypeId -> Id
    public override async Task<ServiceType?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ServiceTypeId AS Id, Code, Name, Price, IsActive
            FROM dbo.ServiceTypes
            WHERE ServiceTypeId = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<ServiceType>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public override async Task<IReadOnlyList<ServiceType>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ServiceTypeId AS Id, Code, Name, Price, IsActive
            FROM dbo.ServiceTypes
            WHERE IsDeleted = 0 AND IsActive = 1
            ORDER BY Name";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<ServiceType>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return rows.AsList();
    }

    public async Task<ServiceType?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ServiceTypeId AS Id, Code, Name, Price, IsActive
            FROM dbo.ServiceTypes
            WHERE Code = @Code AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<ServiceType>(
            new CommandDefinition(sql, new { Code = code }, Transaction, cancellationToken: ct));
    }

    public override async Task<long> AddAsync(ServiceType entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT dbo.ServiceTypes (Code, Name, Price)
            OUTPUT INSERTED.ServiceTypeId
            VALUES (@Code, @Name, @Price)";

        await EnsureOpenAsync(ct);
        try
        {
            return await Connection.ExecuteScalarAsync<long>(
                new CommandDefinition(sql, entity, Transaction, cancellationToken: ct));
        }
        catch (SqlException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }
}
