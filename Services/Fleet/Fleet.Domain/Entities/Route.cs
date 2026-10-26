using Fleet.Domain.Exceptions;

namespace Fleet.Domain.Entities;

public class Route : AuditableEntity
{
    public long Id { get; private set; }
    public long CourierId { get; private set; }
    public long VehicleId { get; private set; }
    public long ZoneId { get; private set; }
    public DateOnly PlannedDate { get; private set; }
    public string Status { get; private set; } = "Planned";
    public decimal PlannedDistanceKm { get; private set; }
    public byte[] RowVersion { get; private set; } = [];
    public Courier Courier { get; private set; } = null!;
    public Vehicle Vehicle { get; private set; } = null!;
    public DeliveryZone Zone { get; private set; } = null!;
    private Route() { }
    public Route(long courierId, long vehicleId, long zoneId, DateOnly plannedDate, decimal plannedDistanceKm)
    {
        if (plannedDistanceKm < 0) throw new DomainException("Planned distance cannot be negative");
        CourierId = courierId; VehicleId = vehicleId; ZoneId = zoneId; PlannedDate = plannedDate; PlannedDistanceKm = plannedDistanceKm;
    }
    public void Start() { if (Status != "Planned") throw new DomainException("Only planned route can start"); Status = "InProgress"; }
    public void Complete() { if (Status != "InProgress") throw new DomainException("Only in-progress route can complete"); Status = "Completed"; }
    public void Cancel() { if (Status is not ("Planned" or "InProgress")) throw new DomainException("Only planned or in-progress route can be cancelled"); Status = "Cancelled"; }
}
