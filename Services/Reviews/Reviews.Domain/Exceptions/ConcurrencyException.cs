namespace Reviews.Domain.Exceptions;

/// <summary>Виняток конфлікту оптимістичного блокування при паралельній модифікації.</summary>
public sealed class ConcurrencyException : DomainException
{
    public ConcurrencyException(string message) : base(message) { }
}

