namespace Shipping.Domain.Exceptions;

/// <summary>Виняток бізнес-конфлікту або недопустимого стану операції.</summary>
public sealed class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message) { }
}

