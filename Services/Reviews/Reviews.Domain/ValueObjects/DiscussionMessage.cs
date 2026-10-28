using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;

namespace Reviews.Domain.ValueObjects;

/// <summary>Незмінне повідомлення обговорення.</summary>
public sealed class DiscussionMessage : ValueObject
{
    public string Author { get; }
    public string Text { get; }
    public DateTime SentAt { get; }

    public DiscussionMessage(string author, string text, DateTime? sentAt = null)
    {
        if (string.IsNullOrWhiteSpace(author))
            throw new DomainValidationException("Автор повідомлення не може бути порожнім.");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainValidationException("Текст повідомлення не може бути порожнім.");

        Author = author.Trim();
        Text = text.Trim();
        SentAt = sentAt ?? DateTime.UtcNow;
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Author;
        yield return Text;
        yield return SentAt;
    }
}

