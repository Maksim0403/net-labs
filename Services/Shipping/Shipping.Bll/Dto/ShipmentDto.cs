namespace Shipping.Bll.Dto;

public record ShipmentDto(
    long Id,
    long CustomerId,
    string TrackingNumber,
    string Status,
    decimal TotalPrice,
    string? RecipientName,
    string? RecipientAddress,
    IReadOnlyList<ShipmentServiceDto> Items);
