using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Repositories;

/// <summary>Контракт репозиторію відгуків.</summary>
public interface IReviewRepository
{
    Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default);
    Task AddAsync(Review review, CancellationToken ct = default);
    Task UpdateAsync(Review review, CancellationToken ct = default);
    Task DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default);
    Task<bool> ExistsForShipmentAndCustomerAsync(long shipmentId, long customerId, CancellationToken ct = default);
}

