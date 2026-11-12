using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Driver;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

public sealed class ProcessedEventRecord
{
    [BsonId]
    public string Id { get; set; } = string.Empty;
    public DateTime ProcessedAt { get; set; } = DateTime.UtcNow;
}

public sealed class ProcessedEventRepository : IProcessedEventRepository
{
    private readonly IMongoCollection<ProcessedEventRecord> _collection;

    public ProcessedEventRepository(MongoDbContext context)
    {
        _collection = context.Database.GetCollection<ProcessedEventRecord>("processed_events");
    }

    public async Task<bool> TryAddAsync(Guid eventId, CancellationToken ct = default)
    {
        try
        {
            var record = new ProcessedEventRecord
            {
                Id = eventId.ToString(),
                ProcessedAt = DateTime.UtcNow
            };
            await _collection.InsertOneAsync(record, cancellationToken: ct);
            return true;
        }
        catch (MongoWriteException ex) when (ex.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }
}

