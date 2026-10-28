using MongoDB.Bson;

namespace Reviews.Domain.Common;

/// <summary>Базова сутність з Id, аудитом і версією для оптимістичної конкурентності.</summary>
public abstract class BaseEntity
{
    public ObjectId Id { get; private set; }
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; private set; }
    public int Version { get; private set; } = 1;

    protected BaseEntity()
    {
        Id = ObjectId.GenerateNewId();
        CreatedAt = DateTime.UtcNow;
        Version = 1;
    }

    protected BaseEntity(ObjectId id)
    {
        Id = id;
        CreatedAt = DateTime.UtcNow;
        Version = 1;
    }

    public void IncrementVersion()
    {
        Version++;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetVersion(int version) => Version = version;
    public void SetUpdatedAt(DateTime updatedAt) => UpdatedAt = updatedAt;
}

