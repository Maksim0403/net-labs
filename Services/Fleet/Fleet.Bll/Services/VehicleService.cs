using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Bll.Specifications;
using Fleet.Dal;
using Fleet.Domain.Entities;
using Fleet.Domain.Exceptions;

namespace Fleet.Bll.Services;

public interface IVehicleService
{
    Task<VehicleDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<VehicleDto> CreateAsync(CreateVehicleDto input, CancellationToken ct = default);
    Task<VehicleDto> ChangeCapacityAsync(long id, decimal capacityKg, CancellationToken ct = default);
}

public sealed class VehicleService(IUnitOfWork uow, IMapper mapper) : IVehicleService
{
    public async Task<VehicleDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var vehicle = await uow.Vehicles.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Vehicle {id} not found");
        return mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> CreateAsync(CreateVehicleDto input, CancellationToken ct = default)
    {
        _ = await uow.Warehouses.GetByIdAsync(input.WarehouseId, ct)
            ?? throw new EntityNotFoundException($"Warehouse {input.WarehouseId} not found");

        var plate = input.PlateNumber.Trim();
        if (await uow.Vehicles.CountAsync(new VehicleByPlateSpec(plate), ct) > 0)
            throw new ConflictException($"Vehicle with plate '{plate}' already exists");

        var vehicle = new Vehicle(input.WarehouseId, plate, input.VehicleType, input.CapacityKg);
        await uow.Vehicles.AddAsync(vehicle, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<VehicleDto>(vehicle);
    }

    public async Task<VehicleDto> ChangeCapacityAsync(long id, decimal capacityKg, CancellationToken ct = default)
    {
        var vehicle = await uow.Vehicles.GetByIdAsync(id, ct)
            ?? throw new EntityNotFoundException($"Vehicle {id} not found");

        vehicle.ChangeCapacity(capacityKg);   // правило живе в сутності
        await uow.SaveChangesAsync(ct);
        return mapper.Map<VehicleDto>(vehicle);
    }
}
