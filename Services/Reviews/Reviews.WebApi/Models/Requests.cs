using Reviews.Application.DTOs;

namespace Reviews.WebApi.Models;

/// <summary>Модель запиту на створення відгуку.</summary>
public sealed record CreateReviewRequest(
    long ShipmentId,
    long CustomerId,
    long CourierId,
    string CourierName,
    string Type,
    int Rating,
    string Text,
    ReviewCriteriaDto? Criteria = null
);

/// <summary>Модель запиту на оновлення відгуку.</summary>
public sealed record UpdateReviewRequest(
    int Rating,
    string Text,
    ReviewCriteriaDto? Criteria,
    int Version
);

/// <summary>Модель запиту на додавання коментаря.</summary>
public sealed record AddCommentRequest(
    string Author,
    string Text,
    int Version
);

