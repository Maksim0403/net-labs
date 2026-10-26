namespace Shipping.Dal.Abstractions;

/// <summary>Спільний CRUD-контракт для репозиторіїв.</summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>М'яке видалення запису (IsDeleted = 1).</summary>
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}

