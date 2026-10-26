namespace Shipping.Domain.Models;

/// <summary>Статуси відправлення.</summary>
public enum ShipmentStatus : byte
{
    Draft = 0,
    Confirmed = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4
}

