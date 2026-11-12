using Platform.EventBus;
using Platform.EventBus.Events;

namespace Fleet.Api.IntegrationEvents;

/// <summary>
/// Публікатор подій каталогу в exchange catalog-events (Лаб. 08, Критерій 5).
/// </summary>
public class CatalogEventPublisher(IEventPublisher publisher)
{
    public Task PublishAsync<T>(T @event, string routingKey, CancellationToken ct = default)
        where T : IntegrationEvent
        => publisher.PublishAsync(@event, "catalog-events", routingKey, ct);
}

