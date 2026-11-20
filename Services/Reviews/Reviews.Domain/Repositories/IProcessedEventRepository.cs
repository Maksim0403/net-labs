namespace Reviews.Domain.Repositories;

/// <summary>Контракт перевірки ідемпотентності для сервісу відгуків.</summary>
public interface IProcessedEventRepository
{
    Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default);
}

