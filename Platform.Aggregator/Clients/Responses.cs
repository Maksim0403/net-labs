namespace Platform.Aggregator.Clients;

internal sealed class ShippingApiResponse
{
    public long Id { get; set; }
    public long CustomerId { get; set; }
    public string TrackingNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string? SenderAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientAddress { get; set; }
    public decimal WeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
    public DateTime? CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public ShippingDetailsApiResponse? Details { get; set; }
}

internal sealed class ShippingDetailsApiResponse
{
    public string? SenderAddress { get; set; }
    public string? RecipientName { get; set; }
    public string? RecipientPhone { get; set; }
    public string? RecipientAddress { get; set; }
    public decimal WeightKg { get; set; }
    public decimal DeclaredValue { get; set; }
}

internal sealed class CourierApiResponse
{
    public long Id { get; set; }
    public long WarehouseId { get; set; }
    public string? WarehouseCode { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Status { get; set; }
    public CourierWarehouseApiResponse? Warehouse { get; set; }
}

internal sealed class CourierWarehouseApiResponse
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

internal sealed class ReviewsApiResponse
{
    public List<ReviewItemApiResponse>? Items { get; set; }
    public long TotalCount { get; set; }
}

internal sealed class ReviewItemApiResponse
{
    public string Id { get; set; } = string.Empty;
    public long ShipmentId { get; set; }
    public long CustomerId { get; set; }
    public int Rating { get; set; }
    public string Text { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
