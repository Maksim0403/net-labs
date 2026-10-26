using MongoDB.Bson;

namespace Reviews.Domain.Common;

/// <summary>Базова сутність з Id, аудитом і версією для оптимістичної конкурентності.</summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; protected set; }
    public DateTime CreatedAt { get; protected set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; protected set; }
    public int Version { get; protected set; } = 1;

    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVersion(int version) => Version = version;
}

