using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;
using Platform.ServiceDefaults;
using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Indices;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.ErrorHandling;
using Reviews.WebApi.Grpc;
using Reviews.WebApi.HealthChecks;
using Reviews.WebApi.Mapping;
using Reviews.WebApi.Metrics;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// gRPC-сервер з серверним interceptor для логування (Частина 4.3, 6.2)
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

// Бізнес-метрики з лічильниками, гістограмами та gauge (Частина 2, Критерії 6, 7)
builder.Services.AddSingleton<ReviewsMetrics>();

// gRPC Health Checking Protocol (Частина 3.4, Критерій 10)
builder.Services.AddGrpcHealthChecks();

// Health check MongoDB (Частина 3.2, Критерій 8)
builder.Services.AddHealthChecks()
    .AddCheck<ReviewsHealthCheck>("reviews-check", tags: ["ready"])
    .AddMongoDb(
        sp => sp.GetRequiredService<IMongoClient>(),
        name: "reviews-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "mongodb", "ready"],
        timeout: TimeSpan.FromSeconds(5));

// Реєстрація шарів Clean Architecture
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ReviewMappingProfile>());

// Обробка помилок через ProblemDetails
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// Контролери та Swagger
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "Reviews.WebApi — Лабораторна робота №04 (Clean Architecture, CQRS, MongoDB)",
        Version = "v1"
    });
});

var app = builder.Build();

app.MapDefaultEndpoints();

// Налаштування індексів та сідінг при запуску
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();

    try
    {
        var indexConfigurator = services.GetRequiredService<IIndexConfigurator>();
        await indexConfigurator.EnsureIndexesAsync();

        var seeder = services.GetRequiredService<IDataSeeder>();
        await seeder.SeedAsync();
    }
    catch (Exception ex)
    {
        logger.LogWarning(ex, "Помилка при ініціалізації індексів або сідінгу MongoDB.");
    }
}

app.UseExceptionHandler();

if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Reviews.WebApi v1");
        c.RoutePrefix = "swagger";
    });
}

app.MapControllers();
app.MapGrpcService<ReviewsGrpcService>();
app.MapGrpcHealthChecksService();

app.Run();
