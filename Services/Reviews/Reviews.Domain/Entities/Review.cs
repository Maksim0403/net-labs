using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Доменна сутність відгуку клієнта про відправлення та кур'єра.</summary>
public sealed class Review : BaseEntity
{
    private readonly List<ReviewComment> _comments = new();

    public long ShipmentId { get; private set; }
    public long CustomerId { get; private set; }
    public long CourierId { get; private set; }
    public string CourierName { get; private set; } = string.Empty;
    public string Type { get; private set; } = "rating";
    public Rating Rating { get; private set; } = null!;
    public string Text { get; private set; } = string.Empty;
    public ReviewCriteria? Criteria { get; private set; }
    public IReadOnlyCollection<ReviewComment> Comments => _comments.AsReadOnly();
    public bool IsDeleted { get; private set; }

    private Review() { }

    public Review(
        long shipmentId,
        long customerId,
        long courierId,
        string courierName,
        string type,
        Rating rating,
        string text,
        ReviewCriteria? criteria = null)
    {
        if (shipmentId <= 0)
            throw new DomainValidationException("Ідентифікатор відправлення має бути більшим за 0.");
        if (customerId <= 0)
            throw new DomainValidationException("Ідентифікатор клієнта має бути більшим за 0.");
        if (courierId <= 0)
            throw new DomainValidationException("Ідентифікатор кур'єра має бути більшим за 0.");
        if (string.IsNullOrWhiteSpace(courierName))
            throw new DomainValidationException("Ім'я кур'єра не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainValidationException("Тип відгуку не може бути порожнім.");

        Id = ObjectId.GenerateNewId();
        ShipmentId = shipmentId;
        CustomerId = customerId;
        CourierId = courierId;
        CourierName = courierName.Trim();
        Type = type.Trim();
        Rating = rating ?? throw new ArgumentNullException(nameof(rating));
        Text = text?.Trim() ?? string.Empty;
        Criteria = criteria;
        CreatedAt = DateTime.UtcNow;
        Version = 1;
        IsDeleted = false;
    }

    public void UpdateDetails(Rating newRating, string newText, ReviewCriteria? newCriteria = null)
    {
        if (IsDeleted)
            throw new DomainConflictException("Неможливо оновити видалений відгук.");

        Rating = newRating ?? throw new ArgumentNullException(nameof(newRating));
        Text = newText?.Trim() ?? string.Empty;
        Criteria = newCriteria;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddComment(string author, string text)
    {
        if (IsDeleted)
            throw new DomainConflictException("Неможливо додати коментар до видаленого відгуку.");

        var comment = new ReviewComment(author, text);
        _comments.Add(comment);
        UpdatedAt = DateTime.UtcNow;
    }

    public void Delete()
    {
        if (IsDeleted)
            throw new DomainConflictException("Відгук уже позначено як видалений.");

        IsDeleted = true;
        UpdatedAt = DateTime.UtcNow;
    }
}

