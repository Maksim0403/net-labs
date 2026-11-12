using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Repositories;

/// <summary>Контракт репозиторію відгуків.</summary>
public interface IReviewRepository : IRepository<Review>
{
    Task<bool> ExistsForShipmentAndCustomerAsync(long shipmentId, long customerId, CancellationToken ct = default);
    Task UpdateCourierNameAsync(long courierId, string newCourierName, CancellationToken ct = default);
    Task UpdateProductNameAsync(int productId, string newProductName, CancellationToken ct = default);
}
