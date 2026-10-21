namespace Shipping.Domain.Models;

/// <summary>1:1 зі Shipment (PK = FK у БД).</summary>
public class ShipmentDetails
{
    public long ShipmentId { get; set; }
    public string SenderAddress { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string RecipientPhone { get; set; } = string.Empty;
    public string RecipientAddress { get; set; } = string.Empty;
    public decimal WeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
}
