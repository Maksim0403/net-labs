namespace Shipping.Domain.Exceptions;

/// <summary>Бізнес-валідація, якій потрібен стан з БД (на відміну від DataAnnotations на DTO).</summary>
public sealed class ValidationException : Exception
{
    public ValidationException(string message) : base(message) { }
}
