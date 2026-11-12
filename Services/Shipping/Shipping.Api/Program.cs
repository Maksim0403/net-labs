using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.EventBus;
using Platform.ServiceDefaults;
using Shipping.Api.ErrorHandling;
using Shipping.Api.Grpc;
using Shipping.Api.HealthChecks;
using Shipping.Api.IntegrationEvents;
using Shipping.Api.Metrics;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Mapping;
using Shipping.Bll.Services;
using Shipping.Dal;
using Shipping.Dal.Abstractions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Підключення до брокера повідомлень RabbitMQ (Лабораторна 08)
builder.AddRabbitMQClient("messaging");
builder.Services.AddEventBus();

// Реєстрація обробників подій та фонового споживача (Consumer)
builder.Services.AddScoped<ProductPriceChangedHandler>();
builder.Services.AddScoped<ProductRenamedHandler>();
builder.Services.AddScoped<CourierUpdatedHandler>();
builder.Services.AddHostedService<CatalogEventsConsumer>();

// gRPC-сервер з серверним interceptor для логування (Частина 4.3, 6.2)
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

// Бізнес-метрики з лічильниками, гістограмами та gauge (Частина 2, Критерії 6, 7)
builder.Services.AddSingleton<ShippingMetrics>();
builder.Services.AddSingleton<OrderMetrics>();

// Рядок підключення з Aspire конфігурації
var connectionString = builder.Configuration.GetConnectionString("ShippingDb")
    ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing");

// gRPC Health Checking Protocol (Частина 3.4, Критерій 10)
builder.Services.AddGrpcHealthChecks();

// Health checks SQL Server (Частина 3.2, Критерій 8)
builder.Services.AddHealthChecks()
    .AddCheck<ShippingHealthCheck>("shipping-check", tags: ["ready"])
    .AddSqlServer(
        connectionString: connectionString,
        name: "orders-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5))
    .AddSqlServer(
        connectionString: connectionString,
        name: "shipping-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5));

// Реєстрація сервісів і Unit of Work
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ShippingProfile>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Централізована обробка помилок (ProblemDetails)
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<ShippingGrpcService>();
app.MapGrpcHealthChecksService();

app.Run();

public partial class Program { }
