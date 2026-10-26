using Ardalis.Specification;
using Fleet.Bll.Dto;
using Fleet.Domain.Entities;

namespace Fleet.Bll.Specifications;

/// <summary>Спільні критерії й сортування, щоб Count і List використовували ті самі умови.</summary>
internal static class CourierSpecs
{
    public static void ApplyCriteria(ISpecificationBuilder<Courier> query, CourierQuery q)
    {
        if (!string.IsNullOrWhiteSpace(q.Search))
        {
            var search = q.Search.Trim();
            query.Where(c => c.FullName.Contains(search));
        }
        if (q.WarehouseId is not null)
        {
            var warehouseId = q.WarehouseId.Value;
            query.Where(c => c.WarehouseId == warehouseId);
        }
        if (!string.IsNullOrWhiteSpace(q.ZoneName))
        {
            var zoneName = q.ZoneName.Trim();
            query.Where(c => c.CourierZones.Any(cz => cz.Zone.Name == zoneName));
        }
    }

    public static void ApplyOrdering(ISpecificationBuilder<Courier> query, CourierQuery q)
    {
        var desc = string.Equals(q.SortDir, "desc", StringComparison.OrdinalIgnoreCase);
        if (string.Equals(q.SortBy, "createdAt", StringComparison.OrdinalIgnoreCase))
        {
            if (desc) query.OrderByDescending(c => c.CreatedAt).ThenBy(c => c.Id);
            else query.OrderBy(c => c.CreatedAt).ThenBy(c => c.Id);
        }
        else
        {
            if (desc) query.OrderByDescending(c => c.FullName).ThenBy(c => c.Id);
            else query.OrderBy(c => c.FullName).ThenBy(c => c.Id);
        }
    }
}

/// <summary>Лише критерії, без Skip/Take: за нею рахується totalCount усіх записів.</summary>
public class CouriersFilterSpec : Specification<Courier>
{
    public CouriersFilterSpec(CourierQuery q) => CourierSpecs.ApplyCriteria(Query, q);
}

/// <summary>Критерії + Include + сортування + Skip/Take: SQL повертає лише одну сторінку.</summary>
public class CouriersPageSpec : Specification<Courier>
{
    public CouriersPageSpec(CourierQuery q, int page, int pageSize)
    {
        CourierSpecs.ApplyCriteria(Query, q);
        Query.Include(c => c.CourierZones).ThenInclude(cz => cz.Zone);
        CourierSpecs.ApplyOrdering(Query, q);
        Query.AsNoTracking();
        Query.Skip((page - 1) * pageSize).Take(pageSize);
    }
}

/// <summary>Нескасовані маршрути кур'єра на дату (правило "не два маршрути на день").</summary>
public class RoutesByCourierAndDateSpec : Specification<Route>
{
    public RoutesByCourierAndDateSpec(long courierId, DateOnly date)
        => Query.Where(r => r.CourierId == courierId && r.PlannedDate == date && r.Status != "Cancelled");
}

public class VehicleByPlateSpec : Specification<Vehicle>
{
    public VehicleByPlateSpec(string plateNumber)
        => Query.Where(v => v.PlateNumber == plateNumber).IgnoreQueryFilters();
}

public class WarehouseByCodeSpec : Specification<Warehouse>
{
    public WarehouseByCodeSpec(string code)
        => Query.Where(w => w.Code == code).IgnoreQueryFilters();
}
