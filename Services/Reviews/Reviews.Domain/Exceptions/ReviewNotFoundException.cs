namespace Reviews.Domain.Exceptions;

/// <summary>Виняток, коли відгук не знайдено.</summary>
public sealed class ReviewNotFoundException : DomainException
{
    public ReviewNotFoundException(string id) : base($"Відгук з ідентифікатором '{id}' не знайдено.") { }
}

