namespace Reviews.Domain.Exceptions;

/// <summary>Виняток порушення правил доменної валідації.</summary>
public sealed class DomainValidationException : DomainException
{
    public DomainValidationException(string message) : base(message) { }
}

