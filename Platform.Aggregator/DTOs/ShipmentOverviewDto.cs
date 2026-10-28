namespace Platform.Aggregator.DTOs;

public sealed class ShipmentOverviewDto
{
    public long ShipmentId { get; set; }
    public string? Status { get; set; }
    public AggregatedShipmentDto? Shipment { get; set; }
    public AggregatedCourierDto? Courier { get; set; }
    public IReadOnlyList<AggregatedReviewDto>? Reviews { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public sealed class AggregatedShipmentDto
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string? SenderAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientAddress { get; set; }
    public decimal WeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
}

public sealed class AggregatedCourierDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Status { get; set; }
    public string? WarehouseCode { get; set; }
    public string? WarehouseName { get; set; }
}

public sealed class AggregatedReviewDto
{
    public string Id { get; set; } = string.Empty;
    public long CustomerId { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
