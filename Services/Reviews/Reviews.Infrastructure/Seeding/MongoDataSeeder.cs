using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Domain.ValueObjects;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Seeding;

/// <summary>Ідемпотентний сідер початкових даних для MongoDB.</summary>
public sealed class MongoDataSeeder : IDataSeeder
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoDataSeeder> _logger;

    public MongoDataSeeder(MongoDbContext context, ILogger<MongoDataSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        try
        {
            var count = await _context.Reviews.CountDocumentsAsync(FilterDefinition<Review>.Empty, cancellationToken: ct);
            if (count > 0)
            {
                _logger.LogInformation("Колекція Reviews вже містить дані ({Count} записів). Сідінг пропущено.", count);
                return;
            }

            _logger.LogInformation("Початок сідінгу колекції Reviews...");

            var r1 = new Review(
                shipmentId: 1,
                customerId: 1,
                courierId: 1,
                courierName: "Андрій Мельник",
                type: "rating",
                rating: new Rating(5),
                text: "Швидко та ввічливо",
                criteria: new ReviewCriteria(5, 5, 4));

            r1.AddComment("support", "Дякуємо за відгук!");

            var r2 = new Review(
                shipmentId: 2,
                customerId: 2,
                courierId: 1,
                courierName: "Андрій Мельник",
                type: "photo_review",
                rating: new Rating(3),
                text: "Пошкоджена коробка",
                criteria: new ReviewCriteria(3, 4, 2));

            await _context.Reviews.InsertManyAsync(new[] { r1, r2 }, cancellationToken: ct);

            _logger.LogInformation("Сідінг Reviews успішно завершено.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося виконати сідінг даних MongoDB (можливо, БД недоступна під час запуску).");
        }
    }
}

