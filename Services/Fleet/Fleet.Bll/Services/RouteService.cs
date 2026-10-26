using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Bll.Specifications;
using Fleet.Dal;
using Fleet.Domain.Entities;
using Fleet.Domain.Exceptions;

namespace Fleet.Bll.Services;

public interface IRouteService
{
    Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RouteDto> PlanAsync(CreateRouteDto input, CancellationToken ct = default);
    Task StartAsync(long id, CancellationToken ct = default);
    Task CompleteAsync(long id, CancellationToken ct = default);
    Task CancelAsync(long id, CancellationToken ct = default);
}

public sealed class RouteService(IUnitOfWork uow, IMapper mapper) : IRouteService
{
    public async Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default)
        => mapper.Map<RouteDto>(await FindAsync(id, ct));

    public async Task<RouteDto> PlanAsync(CreateRouteDto input, CancellationToken ct = default)
    {
        _ = await uow.Couriers.GetByIdAsync(input.CourierId, ct)
            ?? throw new EntityNotFoundException($"Courier {input.CourierId} not found");
        _ = await uow.Vehicles.GetByIdAsync(input.VehicleId, ct)
            ?? throw new EntityNotFoundException($"Vehicle {input.VehicleId} not found");
        _ = await uow.Zones.GetByIdAsync(input.ZoneId, ct)
            ?? throw new EntityNotFoundException($"Zone {input.ZoneId} not found");

        // бізнес-правило зі станом БД, винесене в специфікацію
        if (await uow.Routes.CountAsync(new RoutesByCourierAndDateSpec(input.CourierId, input.PlannedDate), ct) > 0)
            throw new ConflictException($"Courier {input.CourierId} already has a route on {input.PlannedDate:yyyy-MM-dd}");

        var route = new Route(input.CourierId, input.VehicleId, input.ZoneId, input.PlannedDate, input.PlannedDistanceKm);
        await uow.Routes.AddAsync(route, ct);
        await uow.SaveChangesAsync(ct);
        return mapper.Map<RouteDto>(route);
    }

    public async Task StartAsync(long id, CancellationToken ct = default)
    {
        var route = await FindAsync(id, ct);
        route.Start();
        await uow.SaveChangesAsync(ct);
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        var route = await FindAsync(id, ct);
        route.Complete();
        await uow.SaveChangesAsync(ct);
    }

    public async Task CancelAsync(long id, CancellationToken ct = default)
    {
        var route = await FindAsync(id, ct);
        route.Cancel();
        await uow.SaveChangesAsync(ct);
    }

    private async Task<Route> FindAsync(long id, CancellationToken ct)
        => await uow.Routes.GetByIdAsync(id, ct)
           ?? throw new EntityNotFoundException($"Route {id} not found");
}
