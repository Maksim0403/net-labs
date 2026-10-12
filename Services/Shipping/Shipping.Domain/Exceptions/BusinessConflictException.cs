namespace Shipping.Domain.Exceptions;

/// <summary>
/// Недопустима бізнес-операція: недопустимий перехід статусу (SqlException 50012 та інші
/// THROW з процедур Лаб. 01), порушення UNIQUE (2601/2627), конфлікт стану.
/// </summary>
public sealed class BusinessConflictException : Exception
{
    public BusinessConflictException(string message) : base(message) { }
}
