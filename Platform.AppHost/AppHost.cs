var builder = DistributedApplication.CreateBuilder(args);

// Ім'я ресурсу "ShippingDb" збігається з ключем ConnectionStrings:ShippingDb
// у Services/Shipping/Shipping.Api/appsettings.json (Лаб. 01, db/p1) —
// Aspire передасть його в API автоматично через WithReference, без змін
// у коді самого сервісу (Крок F).
//
// Варіант 1 (за замовчуванням тут): БД з Лаб. 01 уже розгорнута локально
// (sqlcmd/SSMS за db/p1/schema.sql + procedures.sql + seed.sql), рядок
// підключення береться з appsettings.json AppHost.
var shippingDb = builder.AddConnectionString("ShippingDb");

// Варіант 2: підняти SQL Server у контейнері замість локального інстансу.
// Тоді перед першим запуском застосуйте до нього db/p1/schema.sql,
// procedures.sql і seed.sql (рядок підключення видно в Aspire Dashboard).
// var sql = builder.AddSqlServer("sql").WithDataVolume();
// var shippingDb = sql.AddDatabase("ShippingDb");

builder.AddProject<Projects.Shipping_Api>("shipping-api")
    .WithReference(shippingDb)
    .WithExternalHttpEndpoints();

builder.Build().Run();
