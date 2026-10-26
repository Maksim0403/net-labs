using Serilog;
using Shipping.Api.ErrorHandling;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Mapping;
using Shipping.Bll.Services;
using Shipping.Dal;
using Shipping.Dal.Abstractions;

var builder = WebApplication.CreateBuilder(args);

// Рядок підключення
var connectionString = builder.Configuration.GetConnectionString("ShippingDb")
    ?? throw new InvalidOperationException("Connection string 'ShippingDb' is missing");

// Логування Serilog
builder.Host.UseSerilog((ctx, cfg) => cfg
    .ReadFrom.Configuration(ctx.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console());

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

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }

