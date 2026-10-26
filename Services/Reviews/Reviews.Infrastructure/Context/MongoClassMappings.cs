using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Conventions;
using MongoDB.Bson.Serialization.IdGenerators;
using Reviews.Domain.Common;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;

namespace Reviews.Infrastructure.Context;

/// <summary>Централізована реєстрація конвенцій та BsonClassMap для доменних сутностей і Value Objects.</summary>
public static class MongoClassMappings
{
    private static readonly object SyncLock = new();
    private static bool _isInitialized;

    public static void Register()
    {
        if (_isInitialized) return;

        lock (SyncLock)
        {
            if (_isInitialized) return;

            // Конвенція camelCase
            var conventionPack = new ConventionPack
            {
                new CamelCaseElementNameConvention(),
                new IgnoreExtraElementsConvention(true)
            };
            ConventionRegistry.Register("camelCase", conventionPack, _ => true);

            // Мапінг базової сутності
            if (!BsonClassMap.IsClassMapRegistered(typeof(BaseEntity)))
            {
                BsonClassMap.RegisterClassMap<BaseEntity>(cm =>
                {
                    cm.AutoMap();
                    cm.MapIdProperty(x => x.Id).SetIdGenerator(ObjectIdGenerator.Instance);
                    cm.MapProperty(x => x.CreatedAt);
                    cm.MapProperty(x => x.UpdatedAt);
                    cm.MapProperty(x => x.Version);
                });
            }

            // Мапінг Value Object: Rating
            if (!BsonClassMap.IsClassMapRegistered(typeof(Rating)))
            {
                BsonClassMap.RegisterClassMap<Rating>(cm =>
                {
                    cm.AutoMap();
                    var ctor = typeof(Rating).GetConstructor(new[] { typeof(int) });
                    if (ctor != null)
                        cm.MapConstructor(ctor, "value");
                });
            }

            // Мапінг Value Object: ReviewComment
            if (!BsonClassMap.IsClassMapRegistered(typeof(ReviewComment)))
            {
                BsonClassMap.RegisterClassMap<ReviewComment>(cm =>
                {
                    cm.AutoMap();
                    var ctor = typeof(ReviewComment).GetConstructor(new[] { typeof(string), typeof(string), typeof(DateTime?) });
                    if (ctor != null)
                        cm.MapConstructor(ctor, "author", "text", "createdAt");
                });
            }

            // Мапінг Value Object: ReviewCriteria
            if (!BsonClassMap.IsClassMapRegistered(typeof(ReviewCriteria)))
            {
                BsonClassMap.RegisterClassMap<ReviewCriteria>(cm =>
                {
                    cm.AutoMap();
                    var ctor = typeof(ReviewCriteria).GetConstructor(new[] { typeof(int), typeof(int), typeof(int) });
                    if (ctor != null)
                        cm.MapConstructor(ctor, "speed", "politeness", "packaging");
                });
            }

            // Мапінг доменної сутності: Review
            if (!BsonClassMap.IsClassMapRegistered(typeof(Review)))
            {
                BsonClassMap.RegisterClassMap<Review>(cm =>
                {
                    cm.AutoMap();
                    // Мапінг приватного поля вкладеної колекції коментарів
                    cm.MapField("_comments").SetElementName("comments");
                });
            }

            // Мапінг доменної сутності: Incident
            if (!BsonClassMap.IsClassMapRegistered(typeof(Incident)))
            {
                BsonClassMap.RegisterClassMap<Incident>(cm =>
                {
                    cm.AutoMap();
                });
            }

            _isInitialized = true;
        }
    }
}

