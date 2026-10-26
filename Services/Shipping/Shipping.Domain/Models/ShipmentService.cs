namespace Shipping.Domain.Models;

/// <summary>Позиція послуги у відправленні.</summary>
public class ShipmentService
{
    public long ShipmentId { get; set; }
    public long ServiceTypeId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ServiceName { get; set; } = string.Empty;
}

