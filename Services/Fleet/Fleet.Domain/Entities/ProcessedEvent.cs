namespace Fleet.Domain.Entities;

/// <summary>Сутність для збереження ідентифікаторів оброблених подій (ідемпотентність).</summary>
public class ProcessedEvent
{
    public Guid EventId { get; set; }
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

