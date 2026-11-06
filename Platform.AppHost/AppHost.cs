var builder = DistributedApplication.CreateBuilder(args);

var password = builder.AddParameter(
    "sql-password",
    builder.Configuration["Parameters:sql-password"] ?? "Your_Strong_Password123!",
    publishValueAsDefault: false,
    secret: true);

var sql = builder.AddSqlServer("sql", password).WithDataVolume();

var shippingDb = sql.AddDatabase("ShippingDb", "ShippingDb");
var fleetDb = sql.AddDatabase("FleetDb", "FleetDb");

var mongoPassword = builder.AddParameter(
    "mongo-password",
    builder.Configuration["Parameters:mongo-password"] ?? "Mongo_Strong_Password123!",
    publishValueAsDefault: false,
    secret: true);

var mongodb = builder.AddMongoDB("mongodb", password: mongoPassword).WithDataVolume();
var trackingDb = mongodb.AddDatabase("trackingdb", "trackingdb");

var databaseInitializer = builder
    .AddProject<Projects.Platform_DatabaseInitializer>("shipping-db-init")
    .WithReference(shippingDb)
    .WaitFor(sql);

var shippingApi = builder.AddProject<Projects.Shipping_Api>("shipping-api")
    .WithReference(shippingDb)
    .WaitForCompletion(databaseInitializer)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

var fleetApi = builder.AddProject<Projects.Fleet_Api>("fleet-api")
    .WithReference(fleetDb)
    .WaitFor(sql)
    .WithExternalHttpEndpoints();

var reviewsWebApi = builder.AddProject<Projects.Reviews_WebApi>("reviews-webapi")
    .WithReference(trackingDb)
    .WaitFor(mongodb)
    .WithExternalHttpEndpoints();

var aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(shippingApi)
    .WithReference(fleetApi)
    .WithReference(reviewsWebApi)
    .WaitFor(shippingApi)
    .WaitFor(fleetApi)
    .WaitFor(reviewsWebApi)
    .WithExternalHttpEndpoints();

var gateway = builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithReference(shippingApi)
    .WithReference(fleetApi)
    .WithReference(reviewsWebApi)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpEndpoint(port: 5000, name: "http")
    .WithExternalHttpEndpoints();

builder.Build().Run();
