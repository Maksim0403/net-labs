using Serilog;
using Shipping.Api.ErrorHandling;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Mapping;
using Shipping.Bll.Services;
using Shipping.Dal;
using Shipping.Dal.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Крок A: рядок підключення з конфігурації (Aspire підставить його за
// назвою ресурсу "ShippingDb" автоматично — Platform.AppHost/AppHost.cs).
var connectionString = builder.Configuration.GetConnectionString("ShippingDb")
    ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing");

// Логування — Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

// DI: Unit of Work — Scoped (один екземпляр / одне з'єднання на HTTP-запит)
builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IShipmentService, ShipmentService>();
builder.Services.AddScoped<ICustomerService, CustomerService>();

builder.Services.AddAutoMapper(cfg => cfg.AddProfile<ShippingProfile>());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Крок D: централізована обробка помилок -> ProblemDetails (RFC 9457)
builder.Services.AddProblemDetails(o => o.CustomizeProblemDetails = ctx =>
    ctx.ProblemDetails.Extensions["traceId"] = ctx.HttpContext.TraceIdentifier);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

var app = builder.Build();

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

// для WebApplicationFactory в майбутніх інтеграційних тестах (Крок E)
public partial class Program { }
