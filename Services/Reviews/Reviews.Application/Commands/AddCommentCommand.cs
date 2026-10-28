using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common;
using Reviews.Application.DTOs;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Commands;

/// <summary>Команда додавання коментаря до відгуку.</summary>
public sealed record AddCommentCommand(
    string ReviewId,
    string Author,
    string Text,
    int Version
) : ICommand<ReviewDto>;

public sealed class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, ReviewDto>
{
    private readonly IReviewRepository _repository;

    public AddCommentCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReviewDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.ReviewId);
        var review = await _repository.GetByIdAsync(objectId, cancellationToken)
            ?? throw new ReviewNotFoundException(request.ReviewId);

        if (review.Version != request.Version)
        {
            throw new ConcurrencyException(
                $"Конфлікт конкурентності для відгуку '{request.ReviewId}'. Очікувана версія {request.Version}, але поточна версія {review.Version}.");
        }

        review.AddComment(request.Author, request.Text);
        await _repository.UpdateAsync(review, cancellationToken);

        return new ReviewDto
        {
            Id = review.Id.ToString(),
            ShipmentId = review.ShipmentId,
            CustomerId = review.CustomerId,
            CourierId = review.CourierId,
            CourierName = review.CourierName,
            Type = review.Type,
            Rating = review.Rating.Value,
            Text = review.Text,
            Criteria = review.Criteria is not null
                ? new ReviewCriteriaDto
                {
                    Speed = review.Criteria.Speed,
                    Politeness = review.Criteria.Politeness,
                    Packaging = review.Criteria.Packaging
                }
                : null,
            Comments = review.Comments.Select(c => new ReviewCommentDto
            {
                Author = c.Author,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Version = review.Version,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}

