using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Configuration;

namespace Reviews.Infrastructure.Context;

/// <summary>Контекст доступу до колекцій MongoDB з ініціалізацією налаштувань через IOptions.</summary>
public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    public MongoDbContext(IOptions<MongoDbSettings> settings, IMongoClient mongoClient)
    {
        MongoClassMappings.Register();

        var dbName = settings.Value.DatabaseName;
        _database = mongoClient.GetDatabase(dbName);
    }

    public IMongoDatabase Database => _database;

    public virtual IMongoCollection<Review> Reviews =>
        _database.GetCollection<Review>("reviews");

    public virtual IMongoCollection<Incident> Incidents =>
        _database.GetCollection<Incident>("incidents");
}

