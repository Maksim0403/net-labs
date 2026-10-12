using System.ComponentModel.DataAnnotations;

namespace Shipping.Bll.Dto;

// Крок C.3: модельна валідація на рівні API через DataAnnotations.
public class CreateShipmentItemDto
{
    [Range(1, long.MaxValue)]
    public long ServiceTypeId { get; set; }

    [Range(1, 100)]
    public int Quantity { get; set; } = 1;
}

public class CreateShipmentDto
{
    [Range(1, long.MaxValue)]
    public long CustomerId { get; set; }

    [Required, StringLength(300, MinimumLength = 3)]
    public string SenderAddress { get; set; } = string.Empty;

    [Required, StringLength(150, MinimumLength = 2)]
    public string RecipientName { get; set; } = string.Empty;

    [Required, Phone]
    public string RecipientPhone { get; set; } = string.Empty;

    [Required, StringLength(300, MinimumLength = 3)]
    public string RecipientAddress { get; set; } = string.Empty;

    [Range(0.01, 1000)]
    public decimal WeightKg { get; set; }

    [Range(0, 1_000_000)]
    public decimal DeclaredValue { get; set; }

    [Required, MinLength(1)]
    public List<CreateShipmentItemDto> Items { get; set; } = new();
}
