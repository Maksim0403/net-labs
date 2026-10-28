using MediatR;
using Reviews.Application.Common;
using Reviews.Application.DTOs;

namespace Reviews.Application.Queries;

/// <summary>Запит отримання пагінованого списку відгуків з фільтрацією.</summary>
public sealed record GetReviewsQuery(
    long? CourierId = null,
    long? CustomerId = null,
    long? ShipmentId = null,
    int? MinRating = null,
    string? SearchText = null,
    int Page = 1,
    int PageSize = 10
) : IQuery<PagedResult<ReviewDto>>;

public sealed class GetReviewsQueryHandler : IRequestHandler<GetReviewsQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public Task<PagedResult<ReviewDto>> Handle(GetReviewsQuery request, CancellationToken cancellationToken)
    {
        return _queries.GetListAsync(request, cancellationToken);
    }
}

