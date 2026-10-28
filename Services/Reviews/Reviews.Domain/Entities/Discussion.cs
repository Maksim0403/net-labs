using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

/// <summary>Доменна сутність обговорення до відгуку.</summary>
public sealed class Discussion : BaseEntity
{
    private readonly List<DiscussionMessage> _messages = new();

    public ObjectId ReviewId { get; private set; }
    public IReadOnlyCollection<DiscussionMessage> Messages => _messages.AsReadOnly();

    private Discussion() : base() { }

    public Discussion(ObjectId reviewId) : base()
    {
        if (reviewId == ObjectId.Empty)
            throw new DomainValidationException("ReviewId не може бути порожнім.");

        ReviewId = reviewId;
    }

    public void AddMessage(string author, string text)
    {
        var message = new DiscussionMessage(author, text);
        _messages.Add(message);
        SetUpdatedAt(DateTime.UtcNow);
    }
}

