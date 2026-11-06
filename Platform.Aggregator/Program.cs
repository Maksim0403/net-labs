using System.Text.Json;
using System.Text.Json.Serialization;
using Platform.Aggregator.Clients;
using Platform.ServiceDefaults;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

