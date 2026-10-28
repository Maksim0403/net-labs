using MongoDB.Bson;
using Reviews.Domain.Common;

namespace Reviews.Domain.Repositories;

/// <summary>Базовий контракт репозиторію для доменних сутностей.</summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default);
}

