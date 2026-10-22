using Fleet.Domain.Exceptions;

namespace Fleet.Domain.Entities;

public class Courier : AuditableEntity
{
    public long Id { get; private set; }
    public string FullName { get; private set; } = "";
    public string Phone { get; private set; } = "";
    public long WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public byte[] RowVersion { get; private set; } = [];
    public CourierLicense? License { get; private set; }
    public ICollection<CourierZone> CourierZones { get; private set; } = new List<CourierZone>();
    public ICollection<Route> Routes { get; private set; } = new List<Route>();
    private Courier() { }
    public Courier(long warehouseId, string fullName, string phone)
    {
        if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone)) throw new DomainException("Courier fields are required");
        WarehouseId = warehouseId; FullName = fullName; Phone = phone;
    }
    public void AssignLicense(CourierLicense license) => License = license;
    public void AssignToZone(DeliveryZone zone)
    {
        if (CourierZones.Any(x => x.ZoneId == zone.Id)) throw new DomainException("Courier is already assigned to this zone");
        CourierZones.Add(new CourierZone(this, zone));
    }
}
