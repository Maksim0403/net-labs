using System.Text.Json;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;

namespace Platform.EventBus;

public interface IEventPublisher
{
    Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent;
}

// IEventPublisher з persistent-публікацією в JSON (Частина 2.2, Критерій 5)
public class RabbitMqEventPublisher(IConnection connection, ILogger<RabbitMqEventPublisher> logger)
    : IEventPublisher
{
    public async Task PublishAsync<T>(T @event, string exchange, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent
    {
        await using var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = @event.EventId.ToString(),
            Type = typeof(T).Name,
            Headers = new Dictionary<string, object?> { ["x-correlation-id"] = @event.CorrelationId }
        };
        TracePropagation.Inject(props);
        var body = JsonSerializer.SerializeToUtf8Bytes(@event, JsonDefaults.Options);
        await channel.BasicPublishAsync(exchange, routingKey, mandatory: false, props, body, ct);
        logger.LogInformation("Published {EventType} {EventId} to {Exchange}/{RoutingKey}, CorrelationId: {CorrelationId}",
            typeof(T).Name, @event.EventId, exchange, routingKey, @event.CorrelationId);
    }
}

