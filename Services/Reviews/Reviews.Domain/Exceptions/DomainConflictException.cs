namespace Reviews.Domain.Exceptions;

/// <summary>Виняток бізнес-конфлікту або дублювання даних.</summary>
public sealed class DomainConflictException : DomainException
{
    public DomainConflictException(string message) : base(message) { }
    public DomainConflictException(string message, Exception innerException) : base(message, innerException) { }
}

