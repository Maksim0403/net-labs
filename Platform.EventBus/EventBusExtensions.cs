using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace Platform.EventBus;

public interface ICorrelationContext
{
    string Current { get; }
}

public class CorrelationContext(IHttpContextAccessor? httpContextAccessor = null) : ICorrelationContext
{
    public string Current =>
        httpContextAccessor?.HttpContext?.Items["X-Correlation-Id"]?.ToString()
        ?? httpContextAccessor?.HttpContext?.Request?.Headers["X-Correlation-Id"].FirstOrDefault()
        ?? Activity.Current?.Id
        ?? Guid.NewGuid().ToString("D");
}

public class TopologyHostedService(IConnection connection, ILogger<TopologyHostedService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var channel = await connection.CreateChannelAsync(cancellationToken: stoppingToken);
            await Topology.Topology.DeclareTopologyAsync(channel, stoppingToken);
            logger.LogInformation("RabbitMQ topology declared successfully");
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not declare RabbitMQ topology during startup");
        }
    }
}

public static class EventBusExtensions
{
    public static IServiceCollection AddEventBus(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICorrelationContext, CorrelationContext>();
        services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
        services.AddHostedService<TopologyHostedService>();
        return services;
    }
}
