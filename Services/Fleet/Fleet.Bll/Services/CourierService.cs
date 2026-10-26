using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Bll.Specifications;
using Fleet.Dal;
using Fleet.Domain.Entities;
using Fleet.Domain.Exceptions;

namespace Fleet.Bll.Services;

public interface ICourierService
{
    Task<CourierDetailsDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<PagedResult<CourierDto>> GetListAsync(CourierQuery query, CancellationToken ct = default);
    Task<CourierDto> CreateAsync(CreateCourierDto input, CancellationToken ct = default);
    Task AssignToZoneAsync(long courierId, long zoneId, CancellationToken ct = default);
    Task<IReadOnlyList<CourierDto>> GetAssignedToZoneSinceAsync(string zoneName, DateTime since, CancellationToken ct = default);
}

public sealed class CourierService(IUnitOfWork uow, IMapper mapper) : ICourierService
{
    // Eager Loading: один запит з усім пов'язаним
    public async Task<CourierDetailsDto> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var courier = await uow.Couriers.GetWithDetailsAsync(id, ct)
            ?? throw new EntityNotFoundException($"Courier {id} not found");
        return mapper.Map<CourierDetailsDto>(courier);
    }

    // Фільтрація, сортування й Skip/Take виконуються в SQL через специфікації
    public async Task<PagedResult<CourierDto>> GetListAsync(CourierQuery query, CancellationToken ct = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var total = await uow.Couriers.CountAsync(new CouriersFilterSpec(query), ct);
        var items = await uow.Couriers.ListAsync(new CouriersPageSpec(query, page, pageSize), ct);

        return new PagedResult<CourierDto>(mapper.Map<List<CourierDto>>(items), total, page, pageSize);
    }

    public async Task<CourierDto> CreateAsync(CreateCourierDto input, CancellationToken ct = default)
    {
        _ = await uow.Warehouses.GetByIdAsync(input.WarehouseId, ct)
            ?? throw new EntityNotFoundException($"Warehouse {input.WarehouseId} not found");

        // правило, якому потрібні дані з БД
        if (await uow.Couriers.ExistsByPhoneAsync(input.Phone, ct))
            throw new ConflictException($"Courier with phone '{input.Phone}' already exists");

        var courier = new Courier(input.WarehouseId, input.FullName.Trim(), input.Phone.Trim());
        await uow.Couriers.AddAsync(courier, ct);
        await uow.SaveChangesAsync(ct);   // один виклик наприкінці операції
        return mapper.Map<CourierDto>(courier);
    }

    public async Task AssignToZoneAsync(long courierId, long zoneId, CancellationToken ct = default)
    {
        var courier = await uow.Couriers.GetWithDetailsAsync(courierId, ct)
            ?? throw new EntityNotFoundException($"Courier {courierId} not found");
        var zone = await uow.Zones.GetByIdAsync(zoneId, ct)
            ?? throw new EntityNotFoundException($"Zone {zoneId} not found");

        courier.AssignToZone(zone);       // DomainException -> 409, якщо вже призначений
        await uow.SaveChangesAsync(ct);
    }

    // LINQ to Entities для M:N через проміжну сутність
    public async Task<IReadOnlyList<CourierDto>> GetAssignedToZoneSinceAsync(string zoneName, DateTime since, CancellationToken ct = default)
    {
        var couriers = await uow.CourierZones.GetCouriersAssignedSinceAsync(zoneName, since, ct);
        return mapper.Map<List<CourierDto>>(couriers);
    }
}
