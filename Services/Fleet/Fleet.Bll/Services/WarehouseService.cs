using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Bll.Specifications;
using Fleet.Dal;
using Fleet.Domain.Entities;
using Fleet.Domain.Exceptions;

namespace Fleet.Bll.Services;

public interface IWarehouseService
{
    Task<WarehouseWithVehiclesDto> GetWithVehiclesAsync(long id, CancellationToken ct = default);
    Task<WarehouseDto> CreateAsync(CreateWarehouseDto input, CancellationToken ct = default);
}

public sealed class WarehouseService(IUnitOfWork uow, IMapper mapper) : IWarehouseService
{
    // Explicit Loading: склад окремо, транспорт окремим запитом
    public async Task<WarehouseWithVehiclesDto> GetWithVehiclesAsync(long id, CancellationToken ct = default)
    {
        var warehouse = await uow.Warehouses.GetWithVehiclesAsync(id, ct)
            ?? throw new EntityNotFoundException($"Warehouse {id} not found");
        return mapper.Map<WarehouseWithVehiclesDto>(warehouse);
    }

    public async Task<WarehouseDto> CreateAsync(CreateWarehouseDto input, CancellationToken ct = default)
    {
        var code = input.Code.Trim();
        if (await uow.Warehouses.CountAsync(new WarehouseByCodeSpec(code), ct) > 0)
            throw new ConflictException($"Warehouse with code '{code}' already exists");

        var warehouse = new Warehouse(code, input.City.Trim(), input.Address.Trim());
        await uow.Warehouses.AddAsync(warehouse, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<WarehouseDto>(warehouse);
    }
}
