using Fleet.Api.ErrorHandling;
using Fleet.Bll.Mapping;
using Fleet.Bll.Services;
using Fleet.Bll.Validators;
using Fleet.Dal;
using Fleet.Dal.Repositories;
using Fleet.Dal.Seeding;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Context;
using Serilog.Events;

var builder = WebApplication.CreateBuilder(args);

// Serilog зі структурованими властивостями та логуванням SQL
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore.Database.Command", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console());

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
    await db.Database.MigrateAsync();
    if (!app.Environment.IsProduction())
    {
        await DbInitializer.SeedAsync(db);
    }
}

app.UseExceptionHandler();
app.UseSerilogRequestLogging();

app.Use(async (context, next) =>
{
    var correlationId = context.Request.Headers["X-Correlation-ID"].FirstOrDefault()
                        ?? context.TraceIdentifier;
    using (LogContext.PushProperty("CorrelationId", correlationId))
    using (LogContext.PushProperty("RequestPath", context.Request.Path))
    {
        await next();
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }

