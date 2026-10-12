using System.Data;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;
using Shipping.Dal.Common;
using Shipping.Domain.Models;

namespace Shipping.Dal.Repositories;

/// <summary>
/// Репозиторій на чистому ADO.NET (Крок B.2): ручне відкриття з'єднання,
/// параметризовані SqlCommand, ручне читання SqlDataReader. Свідомо НЕ
/// успадковує BaseDapperRepository/Generic Repository.
/// </summary>
public sealed class CustomerRepository : RepositoryBase, ICustomerRepository
{
    public CustomerRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction) { }

    public async Task<Customer?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT CustomerId, FullName, Phone, Email, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.Customers
            WHERE CustomerId = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;   // лише параметр, без конкатенації

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return Map(reader);
    }

    public async Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT CustomerId, FullName, Phone, Email, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.Customers
            WHERE Phone = @Phone AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = phone;

        await using var reader = await command.ExecuteReaderAsync(ct);
        if (!await reader.ReadAsync(ct)) return null;
        return Map(reader);
    }

    public async Task<IReadOnlyList<Customer>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT CustomerId, FullName, Phone, Email, CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.Customers
            WHERE IsDeleted = 0
            ORDER BY FullName";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        await using var reader = await command.ExecuteReaderAsync(ct);

        var result = new List<Customer>();
        while (await reader.ReadAsync(ct))
            result.Add(Map(reader));
        return result;
    }

    public async Task<long> AddAsync(Customer entity, CancellationToken ct = default)
    {
        const string sql = @"
            INSERT dbo.Customers (FullName, Phone, Email)
            OUTPUT INSERTED.CustomerId
            VALUES (@FullName, @Phone, @Email)";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 150).Value = entity.FullName;
        command.Parameters.Add("@Phone", SqlDbType.NVarChar, 20).Value = entity.Phone;
        command.Parameters.Add("@Email", SqlDbType.NVarChar, 150).Value = (object?)entity.Email ?? DBNull.Value;

        try
        {
            var id = await command.ExecuteScalarAsync(ct);
            return Convert.ToInt64(id);
        }
        catch (SqlException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task<bool> DeleteAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
               SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME()
             WHERE CustomerId = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = id;

        var affected = await command.ExecuteNonQueryAsync(ct);
        return affected > 0;
    }

    public async Task TouchAsync(long customerId, string updatedBy, CancellationToken ct = default)
    {
        const string sql = @"
            UPDATE dbo.Customers
               SET UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy
             WHERE CustomerId = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        await using var command = new SqlCommand(sql, Connection, Transaction);
        command.Parameters.Add("@Id", SqlDbType.BigInt).Value = customerId;
        command.Parameters.Add("@UpdatedBy", SqlDbType.NVarChar, 100).Value = updatedBy;
        await command.ExecuteNonQueryAsync(ct);
    }

    private static Customer Map(SqlDataReader reader) => new()
    {
        Id = reader.GetInt64(reader.GetOrdinal("CustomerId")),
        FullName = reader.GetString(reader.GetOrdinal("FullName")),
        Phone = reader.GetString(reader.GetOrdinal("Phone")),
        Email = reader.IsDBNull(reader.GetOrdinal("Email")) ? null : reader.GetString(reader.GetOrdinal("Email")),
        CreatedAt = reader.GetDateTime(reader.GetOrdinal("CreatedAt")),
        UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : reader.GetDateTime(reader.GetOrdinal("UpdatedAt")),
        IsDeleted = reader.GetBoolean(reader.GetOrdinal("IsDeleted"))
    };
}
