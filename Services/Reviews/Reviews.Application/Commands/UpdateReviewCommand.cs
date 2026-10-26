using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common;
using Reviews.Application.DTOs;
using Reviews.Domain.Exceptions;
using Reviews.Domain.Repositories;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands;

/// <summary>Команда оновлення відгуку.</summary>
public sealed record UpdateReviewCommand(
    string Id,
    int Rating,
    string Text,
    ReviewCriteriaDto? Criteria,
    int Version
) : ICommand<ReviewDto>;

public sealed class UpdateReviewCommandHandler : IRequestHandler<UpdateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _repository;

    public UpdateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReviewDto> Handle(UpdateReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        var review = await _repository.GetByIdAsync(objectId, cancellationToken)
            ?? throw new ReviewNotFoundException(request.Id);

        if (review.Version != request.Version)
        {
            throw new ConcurrencyException(
                $"Конфлікт конкурентності для відгуку '{request.Id}'. Очікувана версія {request.Version}, але поточна версія {review.Version}.");
        }

        var rating = new Rating(request.Rating);
        var criteria = request.Criteria is not null
            ? new ReviewCriteria(request.Criteria.Speed, request.Criteria.Politeness, request.Criteria.Packaging)
            : null;

        review.UpdateDetails(rating, request.Text, criteria);

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

