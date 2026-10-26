namespace Fleet.Domain.Entities;

public class CourierZone
{
    public long CourierId { get; private set; }
    public long ZoneId { get; private set; }
    public DateTime AssignedAt { get; private set; }
    public Courier Courier { get; private set; } = null!;
    public DeliveryZone Zone { get; private set; } = null!;
    private CourierZone() { }
    public CourierZone(Courier courier, DeliveryZone zone) { Courier = courier; Zone = zone; CourierId = courier.Id; ZoneId = zone.Id; AssignedAt = DateTime.UtcNow; }
}
