namespace Shipping.Dal.Abstractions;

/// <summary>Репозиторій локальної копії довідника товарів (read model) за політикою дублювання.</summary>
public interface IProductRepository
{
    Task UpdateSnapshotAsync(int productId, string name, decimal price, CancellationToken ct = default);
}

