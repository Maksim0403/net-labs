namespace Shipping.Dal.Abstractions;

/// <summary>Репозиторій для перевірки ідемпотентності обробки подій.</summary>
public interface IProcessedEventRepository
{
    Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default);
}

