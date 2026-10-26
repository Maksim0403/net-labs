namespace Fleet.Domain.Entities;

public class DeliveryZone : AuditableEntity
{
    public long Id { get; private set; }
    public string Name { get; private set; } = "";
    public ICollection<CourierZone> CourierZones { get; private set; } = new List<CourierZone>();
    public ICollection<Route> Routes { get; private set; } = new List<Route>();
    private DeliveryZone() { }
    public DeliveryZone(string name) => Name = name;
}
