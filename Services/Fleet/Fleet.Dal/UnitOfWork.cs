using Fleet.Domain.Entities;
using Fleet.Dal.Repositories;

namespace Fleet.Dal;

public interface IUnitOfWork
{
    IWarehouseRepository Warehouses { get; } IGenericRepository<Vehicle> Vehicles { get; } ICourierRepository Couriers { get; } IGenericRepository<DeliveryZone> Zones { get; } IGenericRepository<Route> Routes { get; } ICourierZoneRepository CourierZones { get; } Task<int> SaveChangesAsync(CancellationToken ct = default);
}
public sealed class UnitOfWork(FleetDbContext context) : IUnitOfWork
{
    public IWarehouseRepository Warehouses { get; } = new WarehouseRepository(context); public IGenericRepository<Vehicle> Vehicles { get; } = new GenericRepository<Vehicle>(context); public ICourierRepository Couriers { get; } = new CourierRepository(context); public IGenericRepository<DeliveryZone> Zones { get; } = new GenericRepository<DeliveryZone>(context); public IGenericRepository<Route> Routes { get; } = new GenericRepository<Route>(context); public ICourierZoneRepository CourierZones { get; } = new CourierZoneRepository(context); public Task<int> SaveChangesAsync(CancellationToken ct = default) => context.SaveChangesAsync(ct);
}
