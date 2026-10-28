using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

/// <summary>Базовий узагальнений репозиторій для MongoDB з оптимістичною конкурентністю.</summary>
public class MongoRepository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly MongoDbContext Context;
    protected readonly IMongoCollection<T> Collection;

    public MongoRepository(MongoDbContext context, string collectionName)
    {
        Context = context;
        Collection = context.Database.GetCollection<T>(collectionName);
    }

    public virtual async Task<T?> GetByIdAsync(ObjectId id, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.Eq(x => x.Id, id);
        return await Collection.Find(filter).FirstOrDefaultAsync(ct);
    }

    public virtual async Task AddAsync(T entity, CancellationToken ct = default)
    {
        try
        {
            await Collection.InsertOneAsync(entity, cancellationToken: ct);
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DomainConflictException($"Запис {typeof(T).Name} з таким ключем уже існує.", ex);
        }
    }

    public virtual async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        var expectedVersion = entity.Version;
        entity.IncrementVersion();

        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(x => x.Id, entity.Id),
            Builders<T>.Filter.Eq(x => x.Version, expectedVersion)
        );

        var result = await Collection.ReplaceOneAsync(filter, entity, cancellationToken: ct);
        if (result.MatchedCount == 0)
        {
            entity.SetVersion(expectedVersion);
            throw new ConcurrencyException(
                $"Конфлікт оптимістичного блокування: документ {typeof(T).Name} '{entity.Id}' було змінено іншим процесом (очікувана версія {expectedVersion}).");
        }
    }

    public virtual async Task DeleteAsync(ObjectId id, int expectedVersion, CancellationToken ct = default)
    {
        var filter = Builders<T>.Filter.And(
            Builders<T>.Filter.Eq(x => x.Id, id),
            Builders<T>.Filter.Eq(x => x.Version, expectedVersion)
        );

        var result = await Collection.DeleteOneAsync(filter, cancellationToken: ct);
        if (result.DeletedCount == 0)
        {
            var exists = await Collection.Find(Builders<T>.Filter.Eq(x => x.Id, id)).AnyAsync(ct);
            if (!exists)
                throw new ReviewNotFoundException(id.ToString());

            throw new ConcurrencyException(
                $"Конфлікт оптимістичного блокування під час видалення {typeof(T).Name} '{id}' (очікувана версія {expectedVersion}).");
        }
    }
}

