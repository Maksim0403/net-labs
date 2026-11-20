using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Platform.EventBus.Events;
using Platform.EventBus.Serialization;
using Platform.EventBus.Tracing;

namespace Reviews.WebApi.IntegrationEvents;

/// <summary>
/// Фоновий споживач подій для черги reviews.catalog-sync (Лабораторна 08).
/// </summary>
public class ReviewsCatalogEventsConsumer : BackgroundService
{
    private const string QueueName = "reviews.catalog-sync";
    private const int MaxRetries = 3;

    private readonly IConnection _connection;
    private readonly IServiceScopeFactory scopes;
    private readonly ILogger<ReviewsCatalogEventsConsumer> logger;
    private IChannel? _channel;

    public ReviewsCatalogEventsConsumer(
        IConnection connection,
        IServiceScopeFactory scopes,
        ILogger<ReviewsCatalogEventsConsumer> logger)
    {
        _connection = connection;
        this.scopes = scopes;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _channel = await _connection.CreateChannelAsync(cancellationToken: stoppingToken);

            // Декларативна топологія: topic exchange, черги, bindings, DLX/DLQ (Частина 1.3, Критерій 3)
            await Platform.EventBus.Topology.Topology.DeclareTopologyAsync(_channel, stoppingToken);

            // Обмежуємо кількість невичитаних повідомлень на одного споживача
            await _channel.BasicQosAsync(prefetchSize: 0, prefetchCount: 10, global: false, cancellationToken: stoppingToken);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnMessageAsync;

            await _channel.BasicConsumeAsync(
                queue: QueueName,
                autoAck: false,
                consumer: consumer,
                cancellationToken: stoppingToken);

            logger.LogInformation("ReviewsCatalogEventsConsumer started listening on {Queue}", QueueName);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("ReviewsCatalogEventsConsumer stopped gracefully");
        }
    }

    private async Task OnMessageAsync(object sender, BasicDeliverEventArgs ea)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, QueueName);   // Частина 4.2
        try
        {
            var eventType = ea.BasicProperties.Type;
            logger.LogInformation(
                "Received message {MessageId} of type {Type} on queue {Queue}, CorrelationId: {CorrelationId}",
                ea.BasicProperties.MessageId,
                eventType,
                QueueName,
                ea.BasicProperties.CorrelationId);

            await using var scope = scopes.CreateAsyncScope();

            if (eventType == nameof(CourierUpdatedEvent) || eventType?.Contains("Courier") == true)
            {
                var courierEvent = JsonSerializer.Deserialize<CourierUpdatedEvent>(ea.Body.Span, JsonDefaults.Options)
                    ?? throw new JsonException("Empty message");

                await scope.ServiceProvider.GetRequiredService<CourierUpdatedHandler>()
                    .HandleAsync(courierEvent, CancellationToken.None);
            }
            else if (eventType == nameof(ProductRenamedEvent) || eventType?.Contains("Renamed") == true)
            {
                var renameEvent = JsonSerializer.Deserialize<ProductRenamedEvent>(ea.Body.Span, JsonDefaults.Options)
                    ?? throw new JsonException("Empty message");

                await scope.ServiceProvider.GetRequiredService<ProductRenamedHandler>()
                    .HandleAsync(renameEvent, CancellationToken.None);
            }
            else
            {
                logger.LogInformation("Ignored message type {Type} for Reviews service", eventType);
            }

            // Ручне підтвердження після успішної обробки
            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
            logger.LogInformation("Acknowledged message {MessageId}", ea.BasicProperties.MessageId);
        }
        catch (JsonException ex)                                           // постійний збій
        {
            logger.LogError(ex, "Malformed message {MessageId} of type {Type} on queue {Queue}, sending to DLQ",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)                                               // тимчасовий збій
        {
            logger.LogWarning(ex, "Transient failure processing message {MessageId} of type {Type} on queue {Queue}",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName);
            await RetryOrDeadLetterAsync(ea, ex);                          // Частина 5.1
        }
    }

    private async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, Exception ex)
    {
        var headers = ea.BasicProperties.Headers ?? new Dictionary<string, object?>();
        var attempt = 0;
        if (headers.TryGetValue("x-retry-count", out var v) && v != null)
        {
            if (v is byte[] bytes && int.TryParse(System.Text.Encoding.UTF8.GetString(bytes), out var parsedInt))
            {
                attempt = parsedInt;
            }
            else
            {
                attempt = Convert.ToInt32(v);
            }
        }

        if (attempt >= MaxRetries)
        {
            logger.LogError(ex, "Message {MessageId} of type {Type} on queue {Queue} failed after {Attempts} attempts, sending to DLQ",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName, attempt);
            await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);   // -> DLX
            return;
        }

        var props = new BasicProperties(ea.BasicProperties)
        {
            Headers = new Dictionary<string, object?>(headers) { ["x-retry-count"] = attempt + 1 }
        };

        await _channel!.BasicPublishAsync(ea.Exchange, ea.RoutingKey, mandatory: false, props, ea.Body);
        await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        logger.LogWarning(ex, "Message {MessageId} of type {Type} retry {Attempt}/{Max} on queue {Queue}",
            ea.BasicProperties.MessageId, ea.BasicProperties.Type, attempt + 1, MaxRetries, QueueName);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}

