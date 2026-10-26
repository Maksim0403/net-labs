using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using Reviews.Application.Queries;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Configuration;
using Reviews.Infrastructure.Context;
using Reviews.Infrastructure.Indices;
using Reviews.Infrastructure.Queries;
using Reviews.Infrastructure.Repositories;
using Reviews.Infrastructure.Seeding;

namespace Reviews.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Реєстрація інфраструктурних сервісів MongoDB, репозиторіїв та сідерів.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(
            configuration.GetSection(MongoDbSettings.SectionName));

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            return new MongoClient(settings.ConnectionString);
        });

        services.AddScoped<MongoDbContext>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddScoped<IIndexConfigurator, MongoIndexConfigurator>();
        services.AddScoped<IDataSeeder, MongoDataSeeder>();

        return services;
    }
}

