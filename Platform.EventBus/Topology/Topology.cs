using RabbitMQ.Client;

namespace Platform.EventBus.Topology;

// Декларативна топологія: topic exchange, черги, bindings, DLX/DLQ (Частина 1.3, Критерій 3)
public static class Topology
{
    public const string CatalogEventsExchange = "catalog-events";
    public const string CatalogEventsDlx = "catalog-events.dlx";
    public const string OrdersCatalogSyncQueue = "orders.catalog-sync";
    public const string OrdersCatalogSyncDlq = "orders.catalog-sync.dlq";
    public const string ReviewsCatalogSyncQueue = "reviews.catalog-sync";
    public const string ReviewsCatalogSyncDlq = "reviews.catalog-sync.dlq";

    public static async Task DeclareTopologyAsync(IChannel ch, CancellationToken ct = default)
    {
        // 1. Topic Exchange для бізнес-подій
        await ch.ExchangeDeclareAsync(CatalogEventsExchange, ExchangeType.Topic, durable: true, cancellationToken: ct);
        // 2. Dead Letter Exchange (fanout)
        await ch.ExchangeDeclareAsync(CatalogEventsDlx, ExchangeType.Fanout, durable: true, cancellationToken: ct);

        // 3. Черга для сервісу Замовлень (Orders / Shipping)
        await ch.QueueDeclareAsync(OrdersCatalogSyncQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = CatalogEventsDlx },
            cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogSyncQueue, CatalogEventsExchange, routingKey: "product.*", cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogSyncQueue, CatalogEventsExchange, routingKey: "product.#", cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogSyncQueue, CatalogEventsExchange, routingKey: "courier.*", cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogSyncQueue, CatalogEventsExchange, routingKey: "courier.#", cancellationToken: ct);

        // 4. DLQ для Замовлень
        await ch.QueueDeclareAsync(OrdersCatalogSyncDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(OrdersCatalogSyncDlq, CatalogEventsDlx, routingKey: "", cancellationToken: ct);

        // 5. Черга для сервісу Відгуків (Reviews)
        await ch.QueueDeclareAsync(ReviewsCatalogSyncQueue, durable: true, exclusive: false, autoDelete: false,
            arguments: new Dictionary<string, object?> { ["x-dead-letter-exchange"] = CatalogEventsDlx },
            cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogSyncQueue, CatalogEventsExchange, routingKey: "product.*", cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogSyncQueue, CatalogEventsExchange, routingKey: "product.#", cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogSyncQueue, CatalogEventsExchange, routingKey: "courier.*", cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogSyncQueue, CatalogEventsExchange, routingKey: "courier.#", cancellationToken: ct);

        // 6. DLQ для Відгуків
        await ch.QueueDeclareAsync(ReviewsCatalogSyncDlq, durable: true, exclusive: false, autoDelete: false, cancellationToken: ct);
        await ch.QueueBindAsync(ReviewsCatalogSyncDlq, CatalogEventsDlx, routingKey: "", cancellationToken: ct);
    }
}

