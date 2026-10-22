using AutoMapper;
using Fleet.Bll.Dto;
using Fleet.Domain.Entities;

namespace Fleet.Bll.Mapping;

public class FleetProfile : Profile
{
    public FleetProfile()
    {
        CreateMap<Courier, CourierDto>();

        CreateMap<CourierZone, ZoneAssignmentDto>()
            .ForMember(d => d.ZoneName, o => o.MapFrom(s => s.Zone.Name));

        CreateMap<Courier, CourierDetailsDto>()
            .ForMember(d => d.WarehouseCode, o => o.MapFrom(s => s.Warehouse.Code))
            .ForMember(d => d.LicenseNumber, o => o.MapFrom(s => s.License != null ? s.License.Number : null))
            .ForMember(d => d.LicenseCategory, o => o.MapFrom(s => s.License != null ? s.License.Category : null))
            .ForMember(d => d.LicenseExpiresOn, o => o.MapFrom(s => s.License != null ? s.License.ExpiresOn : (DateOnly?)null))
            .ForMember(d => d.Zones, o => o.MapFrom(s => s.CourierZones));

        CreateMap<Warehouse, WarehouseDto>();
        CreateMap<Warehouse, WarehouseWithVehiclesDto>();
        CreateMap<Vehicle, VehicleDto>();
        CreateMap<Route, RouteDto>();
    }
}
