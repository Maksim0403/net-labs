using Platform.EventBus.Events;

namespace Reviews.WebApi.IntegrationEvents;

// Integration events for Reviews service (Критерій 4)
public abstract record IntegrationEvent : Platform.EventBus.Events.IntegrationEvent;

public record ReviewCreatedEvent(string ReviewId, long ShipmentId, long CustomerId, int Rating)
    : Platform.EventBus.Events.ReviewCreatedEvent(ReviewId, ShipmentId, CustomerId, Rating);