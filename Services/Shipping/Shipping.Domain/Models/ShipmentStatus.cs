namespace Shipping.Domain.Models;

/// <summary>
/// Відповідає tinyint-кодам у db/p1/schema.sql (dbo.Shipments.Status).
/// Допустимі переходи контролюються процедурою dbo.usp_ChangeShipmentStatus (Лаб. 01).
/// </summary>
public enum ShipmentStatus : byte
{
    Draft = 0,
    Confirmed = 1,
    InTransit = 2,
    Delivered = 3,
    Cancelled = 4
}
