using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Entities;

/// <summary>Доменна сутність інциденту доставки.</summary>
public sealed class Incident : BaseEntity
{
    public long ShipmentId { get; private set; }
    public string Type { get; private set; } = string.Empty;
    public string Severity { get; private set; } = "medium";
    public string Status { get; private set; } = "open";
    public string Description { get; private set; } = string.Empty;

    private Incident() : base() { }

    public Incident(long shipmentId, string type, string severity, string description) : base()
    {
        if (shipmentId <= 0)
            throw new DomainValidationException("Ідентифікатор відправлення має бути більшим за 0.");
        if (string.IsNullOrWhiteSpace(type))
            throw new DomainValidationException("Тип інциденту є обов'язковим.");
        if (string.IsNullOrWhiteSpace(severity))
            throw new DomainValidationException("Рівень критичності є обов'язковим.");

        ShipmentId = shipmentId;
        Type = type.Trim();
        Severity = severity.Trim();
        Description = description?.Trim() ?? string.Empty;
        Status = "open";
    }

    public void Resolve()
    {
        if (Status == "closed")
            throw new DomainConflictException("Інцидент вже закритий.");

        Status = "closed";
        SetUpdatedAt(DateTime.UtcNow);
    }

    public void UpdateSeverity(string newSeverity)
    {
        if (string.IsNullOrWhiteSpace(newSeverity))
            throw new DomainValidationException("Рівень критичності не може бути порожнім.");

        Severity = newSeverity.Trim();
        SetUpdatedAt(DateTime.UtcNow);
    }
}

