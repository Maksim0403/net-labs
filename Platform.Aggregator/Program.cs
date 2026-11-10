using System.Text.Json;
using System.Text.Json.Serialization;
using Fleet.Grpc;
using Platform.Aggregator.Clients;
using Platform.ServiceDefaults;
using Reviews.Grpc;
using Shipping.Grpc;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis через Aspire-інтеграцію для кешування агрегованих результатів (Частина 5.3)
builder.AddRedisDistributedCache("redis");

AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);

// Реєстрація типізованих gRPC-клієнтів із service discovery (Частина 5.1)
// AddGrpcClient з адресою за логічним іменем — Aspire service discovery працює автоматично
builder.Services.AddGrpcClient<FleetService.FleetServiceClient>(o =>
    o.Address = new Uri("https://fleet-api"));

builder.Services.AddGrpcClient<ShippingService.ShippingServiceClient>(o =>
    o.Address = new Uri("https://shipping-api"));

builder.Services.AddGrpcClient<ReviewsService.ReviewsServiceClient>(o =>
    o.Address = new Uri("https://reviews-webapi"));

// Реєстрація типізованих HTTP-клієнтів через service discovery
builder.Services.AddHttpClient<ShippingClient>(client =>
{
    client.BaseAddress = new Uri("http://shipping-api");
});

builder.Services.AddHttpClient<FleetClient>(client =>
{
    client.BaseAddress = new Uri("http://fleet-api");
});

builder.Services.AddHttpClient<ReviewsClient>(client =>
{
    client.BaseAddress = new Uri("http://reviews-webapi");
});

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

