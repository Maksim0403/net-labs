using System.Diagnostics;
using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Bll.Specifications;
using Fleet.Dal;
using Fleet.Domain.Entities;
using Fleet.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace Fleet.Bll.Services;

public interface IRouteService
{
    Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default);
    Task<RouteDto> PlanAsync(CreateRouteDto input, CancellationToken ct = default);
    Task StartAsync(long id, CancellationToken ct = default);
    Task CompleteAsync(long id, CancellationToken ct = default);
    Task CancelAsync(long id, CancellationToken ct = default);
}

public sealed class RouteService : IRouteService
{
    private static readonly ActivitySource ActivitySource = new("Platform.Diagnostics", "1.0.0");
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<RouteService>? _logger;

    public RouteService(IUnitOfWork uow, IMapper mapper, ILogger<RouteService>? logger = null)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<RouteDto> GetByIdAsync(long id, CancellationToken ct = default)
        => _mapper.Map<RouteDto>(await FindAsync(id, ct));

    public async Task<RouteDto> PlanAsync(CreateRouteDto input, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Fleet.PlanRoute");
        activity?.SetTag("fleet.courier_id", input.CourierId);
        activity?.SetTag("fleet.vehicle_id", input.VehicleId);
        activity?.SetTag("fleet.zone_id", input.ZoneId);

        _ = await _uow.Couriers.GetByIdAsync(input.CourierId, ct)
            ?? throw new EntityNotFoundException($"Courier {input.CourierId} not found");
        _ = await _uow.Vehicles.GetByIdAsync(input.VehicleId, ct)
            ?? throw new EntityNotFoundException($"Vehicle {input.VehicleId} not found");
        _ = await _uow.Zones.GetByIdAsync(input.ZoneId, ct)
            ?? throw new EntityNotFoundException($"Zone {input.ZoneId} not found");

        // бізнес-правило зі станом БД, винесене в специфікацію
        if (await _uow.Routes.CountAsync(new RoutesByCourierAndDateSpec(input.CourierId, input.PlannedDate), ct) > 0)
            throw new ConflictException($"Courier {input.CourierId} already has a route on {input.PlannedDate:yyyy-MM-dd}");

        var route = new Route(input.CourierId, input.VehicleId, input.ZoneId, input.PlannedDate, input.PlannedDistanceKm);
        await _uow.Routes.AddAsync(route, ct);
        await _uow.SaveChangesAsync(ct);

        activity?.SetTag("fleet.route_id", route.Id);
        _logger?.LogInformation("Route planned successfully. RouteId: {RouteId}, CourierId: {CourierId}, VehicleId: {VehicleId}, ZoneId: {ZoneId}",
            route.Id, input.CourierId, input.VehicleId, input.ZoneId);

        return _mapper.Map<RouteDto>(route);
    }

    public async Task StartAsync(long id, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Fleet.StartRoute");
        activity?.SetTag("fleet.route_id", id);

        var route = await FindAsync(id, ct);
        route.Start();
        await _uow.SaveChangesAsync(ct);

        _logger?.LogInformation("Route {RouteId} started successfully", id);
    }

    public async Task CompleteAsync(long id, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Fleet.CompleteRoute");
        activity?.SetTag("fleet.route_id", id);

        var route = await FindAsync(id, ct);
        route.Complete();
        await _uow.SaveChangesAsync(ct);

        _logger?.LogInformation("Route {RouteId} completed successfully", id);
    }

    public async Task CancelAsync(long id, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Fleet.CancelRoute");
        activity?.SetTag("fleet.route_id", id);

        var route = await FindAsync(id, ct);
        route.Cancel();
        await _uow.SaveChangesAsync(ct);

        _logger?.LogInformation("Route {RouteId} cancelled", id);
    }

    private async Task<Route> FindAsync(long id, CancellationToken ct)
        => await _uow.Routes.GetByIdAsync(id, ct)
           ?? throw new EntityNotFoundException($"Route {id} not found");
}
