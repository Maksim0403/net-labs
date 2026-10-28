using Reviews.Application;
using Reviews.Infrastructure;
using Reviews.Infrastructure.Indices;
using Reviews.Infrastructure.Seeding;
using Reviews.WebApi.ErrorHandling;
using Reviews.WebApi.Mapping;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Налаштування Serilog
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});

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

app.UseSerilogRequestLogging();
app.MapControllers();

app.Run();
