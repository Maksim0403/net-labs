using Fleet.Api.ErrorHandling;
using Fleet.Api.Grpc;
using Fleet.Bll.Caching;
using Fleet.Bll.Mapping;
using Fleet.Bll.Services;
using Fleet.Bll.Validators;
using Fleet.Dal;
using Fleet.Dal.Repositories;
using Fleet.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis через Aspire-інтеграцію для розподіленого кешування (Частина 3.1)
builder.AddRedisDistributedCache("redis");

// Локальне кешування з обмеженням розміру (Частина 2.1)
builder.Services.AddMemoryCache(o => o.SizeLimit = 10_000);
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddScoped<ITwoLevelCache, TwoLevelCache>();

// gRPC-сервер з серверним interceptor для логування (Частина 4.3, 6.2)
builder.Services.AddGrpc(o => o.Interceptors.Add<LoggingInterceptor>());

var connectionString = builder.Configuration.GetConnectionString("FleetDb")
    ?? throw new InvalidOperationException("Connection string 'FleetDb' is missing");

// EF Core DbContext
builder.Services.AddDbContext<FleetDbContext>(options =>
    options.UseSqlServer(connectionString));

// Репозиторії та Unit of Work
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// Сервіси BLL
builder.Services.AddScoped<ICourierService, CourierService>();
builder.Services.AddScoped<IRouteService, RouteService>();
builder.Services.AddScoped<IVehicleService, VehicleService>();
builder.Services.AddScoped<IWarehouseService, WarehouseService>();

// AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<FleetProfile>());

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<CreateCourierDtoValidator>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ProblemDetails & ExceptionHandler
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

// Автоматичне застосування міграцій та ідемпотентний сідінг при запуску
await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<FleetDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await db.Database.MigrateAsync();
    if (!app.Environment.IsProduction())
    {
        try
        {
            await DbInitializer.SeedAsync(db);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Помилка при ініціалізації або сідінгу FleetDb.");
        }
    }
}

app.MapDefaultEndpoints();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapGrpcService<FleetGrpcService>();

app.Run();

public partial class Program { }
