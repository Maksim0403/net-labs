using Fleet.Domain.Exceptions;

namespace Fleet.Domain.Entities;

public class Vehicle : AuditableEntity
{
    public long Id { get; private set; }
    public string PlateNumber { get; private set; } = "";
    public string VehicleType { get; private set; } = "Van";
    public decimal CapacityKg { get; private set; }
    public long WarehouseId { get; private set; }
    public Warehouse Warehouse { get; private set; } = null!;
    public byte[] RowVersion { get; private set; } = [];
    private Vehicle() { }
    public Vehicle(long warehouseId, string plateNumber, string vehicleType, decimal capacityKg)
    {
        if (capacityKg <= 0) throw new DomainException("Capacity must be positive");
        WarehouseId = warehouseId; PlateNumber = plateNumber; VehicleType = string.IsNullOrWhiteSpace(vehicleType) ? "Van" : vehicleType; CapacityKg = capacityKg;
    }
    public void ChangeCapacity(decimal capacityKg)
    {
        if (capacityKg <= 0) throw new DomainException("Capacity must be positive");
        CapacityKg = capacityKg;
    }
}
