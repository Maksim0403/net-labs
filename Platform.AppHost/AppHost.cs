using Microsoft.Extensions.Configuration;

var builder = DistributedApplication.CreateBuilder(args);

var useLocalSqlServer = builder.Configuration.GetValue<bool>("UseLocalSqlServer");

if (useLocalSqlServer)
{
    var shippingDb = builder.AddConnectionString("ShippingDb");
    builder.AddProject<Projects.Shipping_Api>("shipping-api")
        .WithReference(shippingDb)
        .WithExternalHttpEndpoints();
}
else
{
    var password = builder.AddParameter(
        "sql-password",
        builder.Configuration["Parameters:sql-password"],
        publishValueAsDefault: false,
        secret: true);
    var sql = builder.AddSqlServer("sql", password).WithDataVolume();
    var shippingDb = sql.AddDatabase("ShippingDb", "ShippingDb");
    var databaseInitializer = builder.AddProject<Projects.Platform_DatabaseInitializer>("shipping-db-init")
        .WithReference(shippingDb)
        .WaitFor(sql);

    builder.AddProject<Projects.Shipping_Api>("shipping-api")
        .WithReference(shippingDb)
        .WaitForCompletion(databaseInitializer)
        .WithExternalHttpEndpoints();
}

builder.Build().Run();
