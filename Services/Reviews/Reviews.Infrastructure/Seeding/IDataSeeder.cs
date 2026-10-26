namespace Reviews.Infrastructure.Seeding;

/// <summary>Контракт сідінгу початкових даних.</summary>
public interface IDataSeeder
{
    Task SeedAsync(CancellationToken ct = default);
}

