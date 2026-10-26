using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var useLocalSqlServer = builder.Configuration.GetValue<bool>("UseLocalSqlServer");

if (useLocalSqlServer)
{
    var shippingDb = builder.AddConnectionString("ShippingDb");

    builder.AddProject<Projects.Shipping_Api>("shipping-api")
        .WithReference(shippingDb)
        .WithExternalHttpEndpoints();

    var fleetDb = builder.AddConnectionString("FleetDb");

    builder.AddProject<Projects.Fleet_Api>("fleet-api")
        .WithReference(fleetDb)
        .WithExternalHttpEndpoints();
}
else
{
    var password = builder.AddParameter(
        "sql-password",
        builder.Configuration["Parameters:sql-password"]!,
        publishValueAsDefault: false,
        secret: true);

    var sql = builder.AddSqlServer("sql", password).WithDataVolume();

    var shippingDb = sql.AddDatabase("ShippingDb", "ShippingDb");
    var fleetDb = sql.AddDatabase("FleetDb", "FleetDb");

    var databaseInitializer = builder
        .AddProject<Projects.Platform_DatabaseInitializer>("shipping-db-init")
        .WithReference(shippingDb)
        .WaitFor(sql);

    builder.AddProject<Projects.Shipping_Api>("shipping-api")
        .WithReference(shippingDb)
        .WaitForCompletion(databaseInitializer)
        .WithExternalHttpEndpoints();

    builder.AddProject<Projects.Fleet_Api>("fleet-api")
        .WithReference(fleetDb)
        .WaitFor(sql)
        .WithExternalHttpEndpoints();
}

builder.AddProject<Projects.Reviews_WebApi>("reviews-webapi")
    .WithExternalHttpEndpoints();

builder.Build().Run();

