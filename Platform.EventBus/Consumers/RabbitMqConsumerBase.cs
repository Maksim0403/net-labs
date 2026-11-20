using System.Text.Json;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Platform.EventBus.Tracing;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace Platform.EventBus.Consumers;

public abstract class RabbitMqConsumerBase : BackgroundService
{
    protected readonly IConnection Connection;
    protected readonly ILogger Logger;
    protected readonly string QueueName;
    protected readonly ushort PrefetchCount;
    protected readonly int MaxRetries;
    protected IChannel? Channel;

    protected RabbitMqConsumerBase(
        IConnection connection,
        ILogger logger,
        string queueName,
        ushort prefetchCount = 10,
        int maxRetries = 3)
    {
        Connection = connection;
        Logger = logger;
        QueueName = queueName;
        PrefetchCount = prefetchCount;
        MaxRetries = maxRetries;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Channel = await Connection.CreateChannelAsync(cancellationToken: stoppingToken);
        await Topology.Topology.DeclareTopologyAsync(Channel, stoppingToken);
        await Channel.BasicQosAsync(prefetchSize: 0, prefetchCount: PrefetchCount, global: false, stoppingToken);

        var consumer = new AsyncEventingBasicConsumer(Channel);
        consumer.ReceivedAsync += OnMessageReceivedAsync;
        await Channel.BasicConsumeAsync(QueueName, autoAck: false, consumer, stoppingToken);
    }

    protected abstract Task ProcessMessageAsync(BasicDeliverEventArgs ea, CancellationToken ct);

    private async Task OnMessageReceivedAsync(object sender, BasicDeliverEventArgs ea)
    {
        using var activity = TracePropagation.StartConsumerActivity(ea, QueueName);
        try
        {
            await ProcessMessageAsync(ea, CancellationToken.None);
            await Channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (JsonException ex)
        {
            Logger.LogError(ex, "Malformed message {MessageId} of type {Type} on queue {Queue}, sending to DLQ",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName);
            await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Transient error processing message {MessageId} of type {Type} on queue {Queue}",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName);
            await RetryOrDeadLetterAsync(ea, ex);
        }
    }

    protected virtual async Task RetryOrDeadLetterAsync(BasicDeliverEventArgs ea, Exception ex)
    {
        var headers = ea.BasicProperties.Headers is not null
            ? new Dictionary<string, object?>(ea.BasicProperties.Headers)
            : new Dictionary<string, object?>();

        var attempt = 0;
        if (headers.TryGetValue("x-retry-count", out var v) && v is not null)
        {
            attempt = Convert.ToInt32(v);
        }

        if (attempt >= MaxRetries)
        {
            Logger.LogError(ex, "Message {MessageId} of type {Type} on queue {Queue} failed after {Attempts} attempts, sending to DLQ",
                ea.BasicProperties.MessageId, ea.BasicProperties.Type, QueueName, attempt);
            await Channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
            return;
        }

        headers["x-retry-count"] = attempt + 1;
        var props = new BasicProperties(ea.BasicProperties)
        {
            Headers = headers
        };

        await Channel!.BasicPublishAsync(ea.Exchange, ea.RoutingKey, mandatory: false, props, ea.Body);
        await Channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        Logger.LogWarning(ex, "Message {MessageId} retry {Attempt}/{Max}",
            ea.BasicProperties.MessageId, attempt + 1, MaxRetries);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        if (Channel is not null)
        {
            await Channel.CloseAsync(cancellationToken);
        }
        await base.StopAsync(cancellationToken);
    }
}

