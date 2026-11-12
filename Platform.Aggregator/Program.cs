using System.Text.Json;
using System.Text.Json.Serialization;
using Fleet.Grpc;
using Grpc.Health.V1;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Aggregator.Clients;
using Platform.Aggregator.HealthChecks;
using Platform.ServiceDefaults;
using Polly;
using Reviews.Grpc;
using Shipping.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis через Aspire-інтеграцію для кешування агрегованих результатів (Частина 5.3)
builder.AddRedisDistributedCache("redis");

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Реєстрація типізованих gRPC-клієнтів із service discovery та RetryPolicy (Критерії 2, 4)
// Критичний сервіс (Fleet/Catalog): більше спроб, чутливіший backoff
builder.Services.AddGrpcClient<FleetService.FleetServiceClient>(o =>
    o.Address = new Uri("https://fleet-api"))
    .ConfigureChannel(channel => channel.ServiceConfig = new ServiceConfig
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromSeconds(0.5),
                    MaxBackoff = TimeSpan.FromSeconds(4),
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable, Grpc.Core.StatusCode.ResourceExhausted }
                }
            }
        }
    });

// Критичний сервіс (Shipping/Orders): більше спроб
builder.Services.AddGrpcClient<ShippingService.ShippingServiceClient>(o =>
    o.Address = new Uri("https://shipping-api"))
    .ConfigureChannel(channel => channel.ServiceConfig = new ServiceConfig
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 5,
                    InitialBackoff = TimeSpan.FromSeconds(0.5),
                    MaxBackoff = TimeSpan.FromSeconds(4),
                    BackoffMultiplier = 2,
                    RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable, Grpc.Core.StatusCode.ResourceExhausted }
                }
            }
        }
    });

// Некритичний сервіс (Reviews): швидко здається (2 спроби) (Критерій 4)
builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o =>
    o.Address = new Uri("https://reviews-webapi"))
    .ConfigureChannel(channel => channel.ServiceConfig = new ServiceConfig
    {
        MethodConfigs =
        {
            new MethodConfig
            {
                Names = { MethodName.Default },
                RetryPolicy = new RetryPolicy
                {
                    MaxAttempts = 2,
                    InitialBackoff = TimeSpan.FromSeconds(0.2),
                    MaxBackoff = TimeSpan.FromSeconds(1),
                    BackoffMultiplier = 1.5,
                    RetryableStatusCodes = { Grpc.Core.StatusCode.Unavailable }
                }
            }
        }
    });

// Реєстрація gRPC HealthClient для перевірки залежностей в Aggregator (Критерій 10)
builder.Services.AddGrpcClient<Health.HealthClient>(o =>
    o.Address = new Uri("https://fleet-api"));

// Реєстрація типізованих HTTP-клієнтів із кастомізованими політиками стійкості (Критерії 1, 3, 4)
// Критичний: Shipping (Orders) — більше спроб, чутливіший breaker
builder.Services.AddHttpClient<ShippingClient>(client =>
{
    client.BaseAddress = new Uri("http://shipping-api");
})
.RemoveAllResilienceHandlers()
.AddStandardResilienceHandler(o =>
{
    o.Retry.MaxRetryAttempts = 5;
    o.Retry.Delay = TimeSpan.FromSeconds(1);
    o.Retry.BackoffType = DelayBackoffType.Exponential;
    o.Retry.UseJitter = true;
    o.Retry.DisableForUnsafeHttpMethods();

    o.CircuitBreaker.FailureRatio = 0.3; // чутливіший для критичного сервісу
    o.CircuitBreaker.MinimumThroughput = 10;
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    o.CircuitBreaker.OnOpened = args =>
    {
        Console.WriteLine($"[ShippingClient] Circuit opened for {args.BreakDuration}");
        return ValueTask.CompletedTask;
    };

    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<OrdersClient>(client => client.BaseAddress = new Uri("http://shipping-api"))
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 5;
        o.Retry.Delay = TimeSpan.FromSeconds(1);
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.Retry.UseJitter = true;
        o.Retry.DisableForUnsafeHttpMethods();
        o.CircuitBreaker.FailureRatio = 0.3;
    });

// Критичний: Fleet (Catalog) — більше спроб, чутливіший breaker
builder.Services.AddHttpClient<FleetClient>(client =>
{
    client.BaseAddress = new Uri("http://fleet-api");
})
.RemoveAllResilienceHandlers()
.AddStandardResilienceHandler(o =>
{
    o.Retry.MaxRetryAttempts = 5;
    o.Retry.Delay = TimeSpan.FromSeconds(1);
    o.Retry.BackoffType = DelayBackoffType.Exponential;
    o.Retry.UseJitter = true;
    o.Retry.DisableForUnsafeHttpMethods();

    o.CircuitBreaker.FailureRatio = 0.3; // чутливіший для критичного сервісу
    o.CircuitBreaker.MinimumThroughput = 10;
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    o.CircuitBreaker.OnOpened = args =>
    {
        Console.WriteLine($"[FleetClient] Circuit opened for {args.BreakDuration}");
        return ValueTask.CompletedTask;
    };

    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(3);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(15);
});

builder.Services.AddHttpClient<CatalogClient>(client => client.BaseAddress = new Uri("http://fleet-api"))
    .RemoveAllResilienceHandlers()
    .AddStandardResilienceHandler(o =>
    {
        o.Retry.MaxRetryAttempts = 5;
        o.Retry.Delay = TimeSpan.FromSeconds(1);
        o.Retry.BackoffType = DelayBackoffType.Exponential;
        o.Retry.UseJitter = true;
        o.Retry.DisableForUnsafeHttpMethods();
        o.CircuitBreaker.FailureRatio = 0.3;
    });

// Некритичний: Reviews — швидко здається, щоб повернути часткову відповідь (Критерій 4)
builder.Services.AddHttpClient<ReviewsClient>(client =>
{
    client.BaseAddress = new Uri("http://reviews-webapi");
})
.RemoveAllResilienceHandlers()
.AddStandardResilienceHandler(o =>
{
    o.Retry.MaxRetryAttempts = 1;
    o.Retry.Delay = TimeSpan.FromSeconds(0.5);
    o.Retry.BackoffType = DelayBackoffType.Exponential;
    o.Retry.UseJitter = true;
    o.Retry.DisableForUnsafeHttpMethods();

    o.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    o.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
    o.CircuitBreaker.FailureRatio = 0.7; // менш чутливий
    o.CircuitBreaker.MinimumThroughput = 10;
    o.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
    o.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(15);
    o.CircuitBreaker.OnOpened = args =>
    {
        Console.WriteLine($"[ReviewsClient] Circuit opened for {args.BreakDuration}");
        return ValueTask.CompletedTask;
    };
});

// Health checks в Aggregator: Redis (Degraded) та downstream gRPC (Критерії 9, 10)
builder.Services.AddHealthChecks()
    .AddRedis(
        redisConnectionString: builder.Configuration.GetConnectionString("redis") ?? "localhost:6379",
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis"],
        timeout: TimeSpan.FromSeconds(3))
    .AddCheck<CatalogHealthCheck>("catalog-api", tags: ["grpc", "downstream", "ready"])
    .AddCheck<FleetGrpcHealthCheck>("fleet-api", tags: ["grpc", "downstream", "ready"]);

// Налаштування серіалізації: camelCase та WhenWritingNull
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.MapDefaultEndpoints();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();

app.Run();

public partial class Program { }

