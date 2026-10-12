using AutoMapper;
using Shipping.Bll.Dto;
using Shipping.Domain.Models;

namespace Shipping.Bll.Mapping;

/// <summary>Крок C.1. Централізований профіль AutoMapper, зареєстрований у Program.cs.</summary>
public class ShippingProfile : Profile
{
    public ShippingProfile()
    {
        CreateMap<Customer, CustomerDto>();

        CreateMap<ShipmentService, ShipmentServiceDto>()
            .ForCtorParam(nameof(ShipmentServiceDto.ServiceTypeId), o => o.MapFrom(s => s.ServiceTypeId))
            .ForCtorParam(nameof(ShipmentServiceDto.ServiceName), o => o.MapFrom(s => s.ServiceName))
            .ForCtorParam(nameof(ShipmentServiceDto.UnitPrice), o => o.MapFrom(s => s.UnitPrice))
            .ForCtorParam(nameof(ShipmentServiceDto.Quantity), o => o.MapFrom(s => s.Quantity));

        CreateMap<Shipment, ShipmentDto>()
            .ForCtorParam(nameof(ShipmentDto.Id), o => o.MapFrom(s => s.Id))
            .ForCtorParam(nameof(ShipmentDto.CustomerId), o => o.MapFrom(s => s.CustomerId))
            .ForCtorParam(nameof(ShipmentDto.TrackingNumber), o => o.MapFrom(s => s.TrackingNumber))
            .ForCtorParam(nameof(ShipmentDto.Status), o => o.MapFrom(s => s.Status.ToString()))
            .ForCtorParam(nameof(ShipmentDto.TotalPrice), o => o.MapFrom(s => s.TotalPrice))
            .ForCtorParam(nameof(ShipmentDto.RecipientName), o => o.MapFrom(s => s.Details != null ? s.Details.RecipientName : null))
            .ForCtorParam(nameof(ShipmentDto.RecipientAddress), o => o.MapFrom(s => s.Details != null ? s.Details.RecipientAddress : null))
            .ForCtorParam(nameof(ShipmentDto.Items), o => o.MapFrom(s => s.Items));
    }
}
