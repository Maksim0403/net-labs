using MediatR;
using Reviews.Application.Common;
using Reviews.Application.DTOs;
using Reviews.Domain.Entities;
using Reviews.Domain.Repositories;
using Reviews.Domain.ValueObjects;

namespace Reviews.Application.Commands;

/// <summary>Команда створення відгуку.</summary>
public sealed record CreateReviewCommand(
    long ShipmentId,
    long CustomerId,
    long CourierId,
    string CourierName,
    string Type,
    int Rating,
    string Text,
    ReviewCriteriaDto? Criteria = null
) : ICommand<ReviewDto>;

public sealed class CreateReviewCommandHandler : IRequestHandler<CreateReviewCommand, ReviewDto>
{
    private readonly IReviewRepository _repository;

    public CreateReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<ReviewDto> Handle(CreateReviewCommand request, CancellationToken cancellationToken)
    {
        var rating = new Rating(request.Rating);
        var criteria = request.Criteria is not null
            ? new ReviewCriteria(request.Criteria.Speed, request.Criteria.Politeness, request.Criteria.Packaging)
            : null;

        var review = new Review(
            request.ShipmentId,
            request.CustomerId,
            request.CourierId,
            request.CourierName,
            request.Type,
            rating,
            request.Text,
            criteria);

        await _repository.AddAsync(review, cancellationToken);

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
            Comments = Array.Empty<ReviewCommentDto>(),
            Version = review.Version,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt
        };
    }
}

