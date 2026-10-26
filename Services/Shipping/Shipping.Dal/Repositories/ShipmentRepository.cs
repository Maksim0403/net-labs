using System.Data;
using Dapper;
using Microsoft.Data.SqlClient;
using Shipping.Dal.Abstractions;
using Shipping.Dal.Common;
using Shipping.Domain.Models;
using DbException = Microsoft.Data.SqlClient.SqlException;

namespace Shipping.Dal.Repositories;

/// <summary>Репозиторій відправлень на Dapper.</summary>
public sealed class ShipmentRepository : BaseDapperRepository<Shipment>, IShipmentRepository
{
    public ShipmentRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "dbo.Shipments", "ShipmentId") { }

    public override async Task<Shipment?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ShipmentId AS Id, CustomerId, TrackingNumber, Status, TotalPrice,
                   AssignedCourierId, AssignedCourierName, AssignedCourierPhone,
                   CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.Shipments
            WHERE ShipmentId = @Id AND IsDeleted = 0";

        await EnsureOpenAsync(ct);
        return await Connection.QuerySingleOrDefaultAsync<Shipment>(
            new CommandDefinition(sql, new { Id = id }, Transaction, cancellationToken: ct));
    }

    public override async Task<IReadOnlyList<Shipment>> GetAllAsync(CancellationToken ct = default)
    {
        const string sql = @"
            SELECT ShipmentId AS Id, CustomerId, TrackingNumber, Status, TotalPrice,
                   AssignedCourierId, AssignedCourierName, AssignedCourierPhone,
                   CreatedAt, UpdatedAt, IsDeleted
            FROM dbo.Shipments
            WHERE IsDeleted = 0
            ORDER BY CreatedAt DESC";

        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Shipment>(
            new CommandDefinition(sql, transaction: Transaction, cancellationToken: ct));
        return rows.AsList();
    }

    /// <summary>Отримання відправлення з деталями та послугами (multi-mapping).</summary>
    public async Task<Shipment?> GetWithItemsAsync(long shipmentId, CancellationToken ct = default)
    {
        const string sql = @"
            SELECT s.ShipmentId AS Id, s.CustomerId, s.TrackingNumber, s.Status, s.TotalPrice,
                   s.AssignedCourierId, s.AssignedCourierName, s.AssignedCourierPhone,
                   s.CreatedAt, s.UpdatedAt, s.IsDeleted,
                   d.ShipmentId, d.SenderAddress, d.RecipientName, d.RecipientPhone,
                   d.RecipientAddress, d.WeightKg, d.DeclaredValue,
                   i.ShipmentId, i.ServiceTypeId, i.Quantity, i.UnitPrice, i.ServiceName
            FROM dbo.Shipments s
            LEFT JOIN dbo.ShipmentDetails  d ON d.ShipmentId = s.ShipmentId
            LEFT JOIN dbo.ShipmentServices i ON i.ShipmentId = s.ShipmentId
            WHERE s.ShipmentId = @ShipmentId AND s.IsDeleted = 0";

        await EnsureOpenAsync(ct);

        Shipment? result = null;
        await Connection.QueryAsync<Shipment, ShipmentDetails, ShipmentService, Shipment>(
            new CommandDefinition(sql, new { ShipmentId = shipmentId }, Transaction, cancellationToken: ct),
            (shipment, details, item) =>
            {
                result ??= shipment;
                result.Details ??= details;
                if (item is not null && item.ServiceTypeId != 0)
                    result.Items.Add(item);
                return result;
            },
            splitOn: "ShipmentId,ShipmentId");

        return result;
    }

    public async Task<IReadOnlyList<Shipment>> GetPageAsync(
        long customerId, byte? status, int page, int pageSize, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        var rows = await Connection.QueryAsync<Shipment>(new CommandDefinition(
            "dbo.usp_GetCustomerShipments",
            new { CustomerId = customerId, Status = status, Page = page, PageSize = pageSize },
            Transaction,
            commandType: CommandType.StoredProcedure,
            cancellationToken: ct));
        return rows.AsList();
    }

    public override async Task<long> AddAsync(Shipment entity, CancellationToken ct = default)
    {
        if (entity.Details is null)
            throw new InvalidOperationException("Shipment.Details є обов'язковим для створення.");
        if (entity.Items.Count == 0)
            throw new InvalidOperationException("Shipment повинен містити хоча б одну позицію (ServiceList).");

        var services = new DataTable();
        services.Columns.Add("ServiceTypeId", typeof(long));
        services.Columns.Add("Quantity", typeof(int));
        foreach (var item in entity.Items)
            services.Rows.Add(item.ServiceTypeId, item.Quantity);

        var parameters = new DynamicParameters();
        parameters.Add("@CustomerId", entity.CustomerId);
        parameters.Add("@SenderAddress", entity.Details.SenderAddress);
        parameters.Add("@RecipientName", entity.Details.RecipientName);
        parameters.Add("@RecipientPhone", entity.Details.RecipientPhone);
        parameters.Add("@RecipientAddress", entity.Details.RecipientAddress);
        parameters.Add("@WeightKg", entity.Details.WeightKg);
        parameters.Add("@DeclaredValue", entity.Details.DeclaredValue);
        parameters.Add("@Services", services.AsTableValuedParameter("dbo.ServiceList"));
        parameters.Add("@CreatedBy", "shipping-api");
        parameters.Add("@ShipmentId", dbType: DbType.Int64, direction: ParameterDirection.Output);
        parameters.Add("@TrackingNumber", dbType: DbType.AnsiStringFixedLength, size: 12, direction: ParameterDirection.Output);

        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_CreateShipment", parameters, Transaction,
                commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (DbException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }

        entity.Id = parameters.Get<long>("@ShipmentId");
        entity.TrackingNumber = parameters.Get<string>("@TrackingNumber");
        return entity.Id;
    }

    public async Task ConfirmAsync(long shipmentId, string confirmedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_ConfirmShipment",
                new { ShipmentId = shipmentId, ConfirmedBy = confirmedBy },
                Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (DbException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task ChangeStatusAsync(long shipmentId, ShipmentStatus newStatus, string changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_ChangeShipmentStatus",
                new { ShipmentId = shipmentId, NewStatus = (byte)newStatus, ChangedBy = changedBy },
                Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (DbException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }

    public async Task CancelAsync(long shipmentId, string changedBy, CancellationToken ct = default)
    {
        await EnsureOpenAsync(ct);
        try
        {
            await Connection.ExecuteAsync(new CommandDefinition(
                "dbo.usp_CancelShipment",
                new { ShipmentId = shipmentId, ChangedBy = changedBy },
                Transaction, commandType: CommandType.StoredProcedure, cancellationToken: ct));
        }
        catch (DbException ex)
        {
            throw SqlExceptionTranslator.Translate(ex);
        }
    }
}

