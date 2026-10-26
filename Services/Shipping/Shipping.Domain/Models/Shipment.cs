namespace Shipping.Domain.Models;

/// <summary>Агрегат-корінь відправлення.</summary>
public class Shipment
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public ShipmentStatus Status { get; set; } = ShipmentStatus.Draft;
    public decimal TotalPrice { get; set; }

    public long? AssignedCourierId { get; set; }
    public string? AssignedCourierName { get; set; }
    public string? AssignedCourierPhone { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }

    public ShipmentDetails? Details { get; set; }
    public List<ShipmentService> Items { get; set; } = new();
}

