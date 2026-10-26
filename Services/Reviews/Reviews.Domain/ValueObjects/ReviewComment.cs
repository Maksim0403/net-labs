using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Незмінний коментар до відгуку.</summary>
public sealed class ReviewComment : ValueObject
{
    public string Author { get; }
    public string Text { get; }
    public DateTime CreatedAt { get; }

    public ReviewComment(string author, string text, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new DomainValidationException("Автор коментаря не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainValidationException("Текст коментаря не може бути порожнім.");

        Author = author.Trim();
        Text = text.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Author;
        yield return Text;
        yield return CreatedAt;
    }
}

