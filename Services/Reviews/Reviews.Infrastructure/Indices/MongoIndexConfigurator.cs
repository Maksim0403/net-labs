using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Indices;

/// <summary>Сервіс створення унікальних, складених і текстових індексів у колекціях MongoDB.</summary>
public sealed class MongoIndexConfigurator : IIndexConfigurator
{
    private readonly MongoDbContext _context;
    private readonly ILogger<MongoIndexConfigurator> _logger;

    public MongoIndexConfigurator(MongoDbContext context, ILogger<MongoIndexConfigurator> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task EnsureIndexesAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Початок налаштування індексів MongoDB...");

        try
        {
            var indexModels = new List<CreateIndexModel<Review>>
            {
                // Складений індекс (кур'єр + дата)
                new(
                    Builders<Review>.IndexKeys.Ascending(r => r.CourierId).Descending(r => r.CreatedAt),
                    new CreateIndexOptions { Name = "ix_courier_created" }
                ),

                // Унікальний індекс (відправлення + клієнт) для запобігання дублікатів відгуків
                new(
                    Builders<Review>.IndexKeys.Ascending(r => r.ShipmentId).Ascending(r => r.CustomerId),
                    new CreateIndexOptions { Name = "ux_shipment_customer", Unique = true }
                ),

                // Звичайний індекс за відправленням
                new(
                    Builders<Review>.IndexKeys.Ascending(r => r.ShipmentId),
                    new CreateIndexOptions { Name = "ix_shipment" }
                ),

                // Текстовий індекс для повнотекстового пошуку за відгуками
                new(
                    Builders<Review>.IndexKeys.Text(r => r.Text),
                    new CreateIndexOptions { Name = "ix_text_search" }
                )
            };

            await _context.Reviews.Indexes.CreateManyAsync(indexModels, ct);

            _logger.LogInformation("Індекси MongoDB успішно створено або перевірено.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Не вдалося автоматично налаштувати індекси MongoDB (можливо, екземпляр недоступний під час старту).");
        }
    }
}

