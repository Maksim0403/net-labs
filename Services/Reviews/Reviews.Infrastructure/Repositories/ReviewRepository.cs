using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

/// <summary>Репозиторій відгуків на базі MongoDB Driver з оптимістичною конкурентністю за полем Version.</summary>
public sealed class ReviewRepository : IReviewRepository
{
    private readonly MongoDbContext _context;

    public ReviewRepository(MongoDbContext context)
    {
        _context = context;
    }

    public async Task<Review?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, id),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        return await _context.Reviews.Find(filter).FirstOrDefaultAsync(ct);
    }

    public async Task AddAsync(Review review, CancellationToken ct = default)
    {
        try
        {
            await _context.Reviews.InsertOneAsync(review, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DomainConflictException(
                $"Відгук для відправлення {review.ShipmentId} від клієнта {review.CustomerId} вже існує.", ex);
        }
    }

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        var expectedVersion = review.Version;
        review.IncrementVersion();

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, review.Id),
            Builders<Review>.Filter.Eq(r => r.Version, expectedVersion),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        var result = await _context.Reviews.ReplaceOneAsync(filter, review, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            review.SetVersion(expectedVersion);
            throw new ConcurrencyException(
                $"Конфлікт оптимістичного блокування: відгук '{review.Id}' було змінено іншим процесом (очікувана версія {expectedVersion}).");
        }
    }

    public async Task DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default)
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

        var result = await _context.Reviews.UpdateOneAsync(filter, update, cancellationToken: ct);

        if (result.MatchedCount == 0)
        {
            var exists = await _context.Reviews.Find(r => r.Id == id).AnyAsync(ct);
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

        return await _context.Reviews.Find(filter).AnyAsync(ct);
    }
}

