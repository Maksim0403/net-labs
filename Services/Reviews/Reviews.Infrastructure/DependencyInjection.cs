using System.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Core.Events;
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
    private static readonly ActivitySource MongoActivitySource = new("MongoDB.Driver", "1.0.0");

    /// <summary>Реєстрація інфраструктурних сервісів MongoDB, репозиторіїв та сідерів.</summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(options =>
        {
            configuration.GetSection(MongoDbSettings.SectionName).Bind(options);

            var aspireConnStr = configuration.GetConnectionString("trackingdb");
            if (!string.IsNullOrWhiteSpace(aspireConnStr))
            {
                options.ConnectionString = aspireConnStr;
                try
                {
                    var mongoUrl = new MongoUrl(aspireConnStr);
                    if (!string.IsNullOrWhiteSpace(mongoUrl.DatabaseName))
                    {
                        options.DatabaseName = mongoUrl.DatabaseName;
                    }
                }
                catch
                {
                    // Fall back to configured DatabaseName
                }
            }
        });

        services.AddSingleton<IMongoClient>(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value;
            var mongoClientSettings = MongoClientSettings.FromConnectionString(settings.ConnectionString);

            mongoClientSettings.ClusterConfigurator = cb =>
            {
                cb.Subscribe<CommandStartedEvent>(e =>
                {
                    var activity = MongoActivitySource.StartActivity($"mongodb.{e.CommandName}");
                    activity?.SetTag("db.system", "mongodb");
                    activity?.SetTag("db.name", e.DatabaseNamespace?.DatabaseName ?? settings.DatabaseName);
                    activity?.SetTag("db.operation", e.CommandName);
                });
                cb.Subscribe<CommandSucceededEvent>(_ =>
                {
                    Activity.Current?.Stop();
                });
                cb.Subscribe<CommandFailedEvent>(e =>
                {
                    var current = Activity.Current;
                    current?.SetStatus(ActivityStatusCode.Error, e.Failure.Message);
                    current?.Stop();
                });
            };

            return new MongoClient(mongoClientSettings);
        });

        services.AddScoped<MongoDbContext>();
        services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<IReviewQueries, ReviewQueries>();
        services.AddScoped<IIndexConfigurator, MongoIndexConfigurator>();
        services.AddScoped<IDataSeeder, MongoDataSeeder>();

        return services;
    }
}
