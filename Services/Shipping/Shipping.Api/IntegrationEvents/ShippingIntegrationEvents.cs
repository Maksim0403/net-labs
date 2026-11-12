using Platform.EventBus.Events;

namespace Shipping.Api.IntegrationEvents;

// Integration events for Shipping (Orders) service (Критерій 4)
public abstract record IntegrationEvent : Platform.EventBus.Events.IntegrationEvent;

public record OrderStatusChangedEvent(long OrderId, string OldStatus, string NewStatus)
    : Platform.EventBus.Events.IntegrationEvent;

public record CourierAssignedEvent(long ShipmentId, long CourierId, string CourierName, string CourierPhone)
    : Platform.EventBus.Events.CourierAssignedEvent(ShipmentId, CourierId, CourierName, CourierPhone);

