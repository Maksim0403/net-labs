namespace Fleet.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; private set; }
    public string CreatedBy { get; private set; } = "";
    public DateTime? UpdatedAt { get; private set; }
    public string? UpdatedBy { get; private set; }
    public bool IsDeleted { get; private set; }
}
