using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Незмінна оцінка від 1 до 5.</summary>
public sealed class Rating : ValueObject
{
    public int Value { get; }

    public Rating(int value)
    {
        if (value is < 1 or > 5)
            throw new DomainValidationException("Оцінка повинна бути в діапазоні від 1 до 5.");

        Value = value;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value.ToString();

    public static implicit operator int(Rating rating) => rating.Value;
    public static explicit operator Rating(int value) => new(value);
}

