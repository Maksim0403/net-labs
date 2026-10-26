namespace Reviews.Infrastructure.Configuration;

/// <summary>Параметри підключення та налаштування бази даних MongoDB.</summary>
public sealed class MongoDbSettings
{
    public const string SectionName = "MongoDb";

    public string ConnectionString { get; set; } = "mongodb://localhost:27017";
    public string DatabaseName { get; set; } = "trackingdb";
}

