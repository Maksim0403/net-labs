using System.Text.RegularExpressions;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Bll.Mapping;
using Shipping.Bll.Services;
using Shipping.Dal;
using Xunit;

namespace Shipping.Tests;

public sealed class ShippingDatabaseIntegrationTests
{
    [SqlIntegrationFact]
    public async Task ShipmentLifecycle_UsesRealDatabaseProceduresAndPersistsEachStatus()
    {
        var configuredConnectionString = Environment.GetEnvironmentVariable("SHIPPING_TEST_CONNECTION_STRING");
        if (string.IsNullOrWhiteSpace(configuredConnectionString))
        {
            return;
        }

        var configured = new SqlConnectionStringBuilder(configuredConnectionString);
        if (!configured.InitialCatalog.Contains("test", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "SHIPPING_TEST_CONNECTION_STRING must target a database whose name contains 'test'.");
        }

        await PrepareDatabaseAsync(configured);
        try
        {
            await using var unitOfWork = new UnitOfWork(configured.ConnectionString);
            var mapper = new MapperConfiguration(
                    configuration => configuration.AddProfile<ShippingProfile>(),
                    NullLoggerFactory.Instance)
                .CreateMapper();
            var customers = new CustomerService(unitOfWork, mapper);
            var shipments = new ShipmentService(unitOfWork, mapper);
            var phone = $"+38050{Random.Shared.Next(1000000, 9999999)}";

            var customer = await customers.CreateAsync(new CreateCustomerDto
            {
                FullName = "Integration Test Customer",
                Phone = phone,
                Email = $"{Guid.NewGuid():N}@example.test"
            });

            var created = await shipments.CreateAsync(new CreateShipmentDto
            {
                CustomerId = customer.Id,
                SenderAddress = "Test sender address",
                RecipientName = "Test recipient",
                RecipientPhone = "+380671234567",
                RecipientAddress = "Test recipient address",
                WeightKg = 1.5m,
                DeclaredValue = 100m,
                Items = [new CreateShipmentItemDto { ServiceTypeId = 1, Quantity = 1 }]
            });

            Assert.Equal("Draft", created.Status);
            Assert.Single(created.Items);

            await shipments.ConfirmAsync(created.Id);
            Assert.Equal("Confirmed", (await shipments.GetByIdAsync(created.Id))!.Status);

            await shipments.CancelAsync(created.Id);
            Assert.Equal("Cancelled", (await shipments.GetByIdAsync(created.Id))!.Status);

            var deliveredShipment = await shipments.CreateAsync(new CreateShipmentDto
            {
                CustomerId = customer.Id,
                SenderAddress = "Test sender address 2",
                RecipientName = "Test recipient 2",
                RecipientPhone = "+380671234568",
                RecipientAddress = "Test recipient address 2",
                WeightKg = 2m,
                DeclaredValue = 200m,
                Items = [new CreateShipmentItemDto { ServiceTypeId = 1, Quantity = 1 }]
            });

            await shipments.ConfirmAsync(deliveredShipment.Id);
            Assert.Equal("Confirmed", (await shipments.GetByIdAsync(deliveredShipment.Id))!.Status);
            await shipments.ChangeStatusAsync(deliveredShipment.Id, "InTransit");
            Assert.Equal("InTransit", (await shipments.GetByIdAsync(deliveredShipment.Id))!.Status);
            await shipments.ChangeStatusAsync(deliveredShipment.Id, "Delivered");
            Assert.Equal("Delivered", (await shipments.GetByIdAsync(deliveredShipment.Id))!.Status);

            await using var verification = new SqlConnection(configured.ConnectionString);
            await verification.OpenAsync();
            await using var command = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.ShipmentStatusHistory WHERE ShipmentId = @ShipmentId AND NewStatus IN (0, 1, 4);",
                verification);
            command.Parameters.AddWithValue("@ShipmentId", created.Id);
            Assert.Equal(3, Convert.ToInt32(await command.ExecuteScalarAsync()));

            await using var deliveredCommand = new SqlCommand(
                "SELECT Status FROM dbo.Shipments WHERE ShipmentId = @ShipmentId;",
                verification);
            deliveredCommand.Parameters.AddWithValue("@ShipmentId", deliveredShipment.Id);
            Assert.Equal(3, Convert.ToInt32(await deliveredCommand.ExecuteScalarAsync()));

            await using var historyCommand = new SqlCommand(
                "SELECT COUNT(*) FROM dbo.ShipmentStatusHistory WHERE ShipmentId = @ShipmentId AND NewStatus IN (0, 1, 2, 3);",
                verification);
            historyCommand.Parameters.AddWithValue("@ShipmentId", deliveredShipment.Id);
            Assert.Equal(4, Convert.ToInt32(await historyCommand.ExecuteScalarAsync()));
        }
        finally
        {
            await DropDatabaseAsync(configured);
        }
    }

internal sealed class SqlIntegrationFactAttribute : FactAttribute
{
    public SqlIntegrationFactAttribute()
    {
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("SHIPPING_TEST_CONNECTION_STRING")))
        {
            Skip = "Set SHIPPING_TEST_CONNECTION_STRING to run SQL integration tests against an isolated test database.";
        }
    }
}

    private static async Task PrepareDatabaseAsync(SqlConnectionStringBuilder configured)
    {
        var master = new SqlConnectionStringBuilder(configured.ConnectionString) { InitialCatalog = "master" };
        await using (var connection = new SqlConnection(master.ConnectionString))
        {
            await connection.OpenAsync();
            await ExecuteBatchesAsync(connection, await ReadScriptAsync("schema.sql", configured.InitialCatalog));
        }

        await using var database = new SqlConnection(configured.ConnectionString);
        await database.OpenAsync();
        await ExecuteBatchesAsync(database, await ReadScriptAsync("procedures.sql", configured.InitialCatalog));
        await ExecuteBatchesAsync(database, await ReadScriptAsync("seed.sql", configured.InitialCatalog));
    }

    private static async Task DropDatabaseAsync(SqlConnectionStringBuilder configured)
    {
        var master = new SqlConnectionStringBuilder(configured.ConnectionString) { InitialCatalog = "master" };
        await using var connection = new SqlConnection(master.ConnectionString);
        await connection.OpenAsync();
        var databaseName = configured.InitialCatalog.Replace("]", "]]", StringComparison.Ordinal);
        await using var command = new SqlCommand(
            $"IF DB_ID(@DatabaseName) IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END",
            connection);
        command.Parameters.AddWithValue("@DatabaseName", configured.InitialCatalog);
        await command.ExecuteNonQueryAsync();
    }

    private static async Task<string> ReadScriptAsync(string fileName, string databaseName)
    {
        var script = await File.ReadAllTextAsync(fileName);
        return script.Replace("ShippingDb", databaseName, StringComparison.Ordinal);
    }

    private static async Task ExecuteBatchesAsync(SqlConnection connection, string script)
    {
        foreach (var batch in Regex.Split(script, @"^\s*GO\s*(?:--.*)?$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(batch))
            {
                continue;
            }

            await using var command = new SqlCommand(batch, connection);
            await command.ExecuteNonQueryAsync();
        }
    }
}