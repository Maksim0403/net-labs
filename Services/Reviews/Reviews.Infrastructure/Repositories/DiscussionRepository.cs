using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

/// <summary>Репозиторій обговорень на базі MongoDB Driver.</summary>
public sealed class DiscussionRepository : MongoRepository<Discussion>, IDiscussionRepository
{
    public DiscussionRepository(MongoDbContext context) : base(context, "discussions")
    {
    }

    public async Task<Discussion?> GetByReviewIdAsync(ObjectId reviewId, CancellationToken ct = default)
    {
        var filter = Builders<Discussion>.Filter.Eq(d => d.ReviewId, reviewId);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }
}

