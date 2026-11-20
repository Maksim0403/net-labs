using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;

namespace Shipping.Dal.Repositories;

/// <summary>Реалізація реплікації знімка товару в базі замовлень/доставки.</summary>
public sealed class ProductRepository : IProductRepository
{
    private readonly SqlConnection _connection;
    private readonly Func<SqlTransaction?> _tx;

    public ProductRepository(SqlConnection connection, Func<SqlTransaction?> tx)
    {
        _connection = connection;
        _tx = tx;
    }

    public async Task UpdateSnapshotAsync(int productId, string name, decimal price, CancellationToken ct = default)
    {
        const string sql = @"
            MERGE INTO dbo.Products AS target
            USING (SELECT @ProductId AS ProductId, @Name AS Name, @Price AS Price) AS source
            ON target.ProductId = source.ProductId
            WHEN MATCHED THEN
                UPDATE SET 
                    Name = CASE WHEN source.Name IS NOT NULL AND source.Name <> '' THEN source.Name ELSE target.Name END,
                    Price = CASE WHEN source.Price > 0 THEN source.Price ELSE target.Price END,
                    UpdatedAt = SYSUTCDATETIME()
            WHEN NOT MATCHED THEN
                INSERT (ProductId, Name, Price, UpdatedAt)
                VALUES (source.ProductId, source.Name, source.Price, SYSUTCDATETIME());";

        var command = new CommandDefinition(sql, new { ProductId = productId, Name = name, Price = price }, transaction: _tx(), cancellationToken: ct);
        await _connection.ExecuteAsync(command);
    }
}
