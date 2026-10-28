namespace Reviews.Domain.Common;

/// <summary>Базовий об'єкт-значення з порівнянням за компонентами рівності.</summary>
public abstract class ValueObject
{
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null || obj.GetType() != GetType())
            return false;

        var other = (ValueObject)obj;
        return GetEqualityComponents().SequenceEqual(other.GetEqualityComponents());
    }

    public override int GetHashCode()
    {
        return GetEqualityComponents()
            .Select(x => x?.GetHashCode() ?? 0)
            .Aggregate(0, (hash, item) => HashCode.Combine(hash, item));
    }

    public static bool operator ==(ValueObject? left, ValueObject? right)
    {
        if (ReferenceEquals(left, null) && ReferenceEquals(right, null))
            return true;
        if (ReferenceEquals(left, null) || ReferenceEquals(right, null))
            return false;
        return left.Equals(right);
    }

    public static bool operator !=(ValueObject? left, ValueObject? right) => !(left == right);
}

