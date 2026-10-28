namespace Reviews.Infrastructure.Indices;

/// <summary>Контракт конфігуратора індексів MongoDB.</summary>
public interface IIndexConfigurator
{
    Task EnsureIndexesAsync(CancellationToken ct = default);
}

