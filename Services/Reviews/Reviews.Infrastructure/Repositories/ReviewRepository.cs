using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.DTOs;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

/// <summary>Репозиторій відгуків на базі MongoDB Driver з оптимістичною конкурентністю та проєкцією в DTO.</summary>
public sealed class ReviewRepository : MongoRepository<Review>, IReviewRepository
{
    public ReviewRepository(MongoDbContext context) : base(context, "reviews")
    {
    }

    public override async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, id),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public override async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.IncrementVersion();

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        var result = await Collection.ReplaceOneAsync(filter, review, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            review.SetVersion(expectedVersion);
            throw new ConcurrencyException(
                $"Конфлікт оптимістичного блокування: відгук '{review.Id}' було змінено іншим процесом (очікувана версія {expectedVersion}).");
        }
    }

    public override async Task DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        var update = Builders<Review>.Update
            .Set(r => r.IsDeleted, true)
            .Set(r => r.UpdatedAt, DateTime.UtcNow)
            .Inc(r => r.Version, 1);

        var result = await Collection.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            var exists = await Collection.Find(r => r.Id == id).AnyAsync(ct);
            if (!exists)
                throw new ReviewNotFoundException(id.ToString());

            throw new ConcurrencyException(
                $"Конфлікт оптимістичного блокування під час видалення відгуку '{id}' (очікувана версія {expectedVersion}).");
        }
    }

    public async Task<bool> ExistsForShipmentAndCustomerAsync(long shipmentId, long customerId, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.ShipmentId, shipmentId),
            Builders<Review>.Filter.Eq(r => r.CustomerId, customerId),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        return await Collection.Find(filter).AnyAsync(ct);
    }

    /// <summary>Читання списку із проєкцією безпосередньо в MongoDB одразу в DTO.</summary>
    public async Task<IReadOnlyList<ReviewDto>> GetProjectedListAsync(
        FilterDefinition<Review> filter,
        int skip,
        int limit,
        CancellationToken ct = default)
    {
        var projection = Builders<Review>.Projection.Expression(r => new ReviewDto
        {
            Id = r.Id.ToString(),
            ShipmentId = r.ShipmentId,
            CustomerId = r.CustomerId,
            CourierId = r.CourierId,
            CourierName = r.CourierName,
            Type = r.Type,
            Rating = r.Rating.Value,
            Text = r.Text,
            Criteria = r.Criteria != null ? new ReviewCriteriaDto
            {
                Speed = r.Criteria.Speed,
                Politeness = r.Criteria.Politeness,
                Packaging = r.Criteria.Packaging
            } : null,
            Comments = r.Comments.Select(c => new ReviewCommentDto
            {
                Author = c.Author,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Version = r.Version,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });

        return await Collection.Find(filter)
            .Project(projection)
            .Skip(skip)
            .Limit(limit)
            .ToListAsync(ct);
    }
}

