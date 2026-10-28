using MongoDB.Bson;
using Reviews.Domain.Entities;

namespace Reviews.Domain.Repositories;

/// <summary>Контракт репозиторію обговорень.</summary>
public interface IDiscussionRepository : IRepository<Discussion>
{
    Task<Discussion?> GetByReviewIdAsync(ObjectId reviewId, CancellationToken ct = default);
}

