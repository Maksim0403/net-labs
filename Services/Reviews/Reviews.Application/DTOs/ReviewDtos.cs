namespace Reviews.Application.DTOs;

/// <summary>DTO відгуку для читання.</summary>
public sealed class ReviewDto
{
    public string Id { get; init; } = string.Empty;
    public long ShipmentId { get; init; }
    public long CustomerId { get; init; }
    public long CourierId { get; init; }
    public string CourierName { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public int Rating { get; init; }
    public string Text { get; init; } = string.Empty;
    public ReviewCriteriaDto? Criteria { get; init; }
    public IReadOnlyList<ReviewCommentDto> Comments { get; init; } = Array.Empty<ReviewCommentDto>();
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? UpdatedAt { get; init; }
}

/// <summary>DTO критеріїв оцінки.</summary>
public sealed class ReviewCriteriaDto
{
    public int Speed { get; init; }
    public int Politeness { get; init; }
    public int Packaging { get; init; }
}

/// <summary>DTO коментаря до відгуку.</summary>
public sealed class ReviewCommentDto
{
    public string Author { get; init; } = string.Empty;
    public string Text { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}

