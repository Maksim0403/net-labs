using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Repositories;

/// <summary>Контракт репозиторію відгуків.</summary>
public interface IReviewRepository : IRepository<Review>
{
    Task<bool> ExistsForShipmentAndCustomerAsync(long shipmentId, long customerId, CancellationToken ct = default);
}

