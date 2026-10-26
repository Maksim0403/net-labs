using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Незмінні критерії оцінювання відгуку (швидкість, ввічливість, пакування).</summary>
public sealed class ReviewCriteria : ValueObject
{
    public int Speed { get; }
    public int Politeness { get; }
    public int Packaging { get; }

    public ReviewCriteria(int speed, int politeness, int packaging)
    {
        ValidateScore(speed, nameof(speed));
        ValidateScore(politeness, nameof(politeness));
        ValidateScore(packaging, nameof(packaging));

        Speed = speed;
        Politeness = politeness;
        Packaging = packaging;
    }

    private static void ValidateScore(int value, string paramName)
    {
        if (value is < 1 or > 5)
            throw new DomainValidationException($"Критерій '{paramName}' має бути в межах від 1 до 5.");
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Speed;
        yield return Politeness;
        yield return Packaging;
    }
}

