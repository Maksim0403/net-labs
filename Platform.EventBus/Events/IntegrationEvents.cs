namespace Platform.EventBus.Events;

// Базовий незмінний integration event з EventId, OccurredAt, CorrelationId (Частина 2.1, Критерій 4)
public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string? CorrelationId { get; init; }
}

public record ProductPriceChangedEvent(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice)
    : IntegrationEvent;

public record ProductRenamedEvent(int ProductId, string OldName, string NewName)
    : IntegrationEvent;

public record CourierUpdatedEvent(long CourierId, string FullName, string Phone, string Status)
    : IntegrationEvent;

public record CourierAssignedEvent(long ShipmentId, long CourierId, string CourierName, string CourierPhone)
    : IntegrationEvent;

public record ReviewCreatedEvent(string ReviewId, long ShipmentId, long CustomerId, int Rating)
    : IntegrationEvent;

