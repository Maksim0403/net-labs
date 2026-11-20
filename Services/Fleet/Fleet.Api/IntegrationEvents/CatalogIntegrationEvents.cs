using Platform.EventBus.Events;

namespace Fleet.Api.IntegrationEvents;

// Незмінні integration events з EventId і CorrelationId (Частина 2.1, Критерій 4)
public abstract record IntegrationEvent : Platform.EventBus.Events.IntegrationEvent;

public record ProductPriceChangedEvent(int ProductId, string ProductName, decimal OldPrice, decimal NewPrice)
    : Platform.EventBus.Events.ProductPriceChangedEvent(ProductId, ProductName, OldPrice, NewPrice);

public record ProductRenamedEvent(int ProductId, string OldName, string NewName)
    : Platform.EventBus.Events.ProductRenamedEvent(ProductId, OldName, NewName);

public record CourierUpdatedEvent(long CourierId, string FullName, string Phone, string Status)
    : Platform.EventBus.Events.CourierUpdatedEvent(CourierId, FullName, Phone, Status);

