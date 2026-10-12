namespace Shipping.Domain.Models;

/// <summary>
/// Позиція M:N Shipments&lt;-&gt;ServiceTypes. ServiceName/UnitPrice — знімок
/// на момент оформлення (політика дублювання, Лаб. 01, docs/dedup-policy.md):
/// не оновлюється навіть якщо довідник ServiceTypes зміниться пізніше.
/// </summary>
public class ShipmentService
{
    public long ShipmentId { get; set; }
    public long ServiceTypeId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}
