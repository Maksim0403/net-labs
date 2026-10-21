namespace Shipping.Dal.Abstractions;

/// <summary>
/// Спільний CRUD-контракт для всіх репозиторіїв. Методи повертають доменні
/// моделі — жоден не повертає SqlDataReader/DataTable/сирі рядки.
/// </summary>
public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken ct = default);
    Task<long> AddAsync(T entity, CancellationToken ct = default);

    /// <summary>М'яке видалення (IsDeleted = 1), узгоджено зі схемою Лаб. 01.</summary>
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
}
