namespace Shipping.Bll.Dto;

public record ShipmentServiceDto(long ServiceTypeId, string ServiceName, decimal UnitPrice, int Quantity);
