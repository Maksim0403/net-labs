using MediatR;
using Reviews.Application.Common;
using Reviews.Application.DTOs;

namespace Reviews.Application.Queries;

/// <summary>Запит отримання відгуку за його ідентифікатором.</summary>
public sealed record GetReviewByIdQuery(string Id) : IQuery<ReviewDto?>;

public sealed class GetReviewByIdQueryHandler : IRequestHandler<GetReviewByIdQuery, ReviewDto?>
{
    private readonly IReviewQueries _queries;

    public GetReviewByIdQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public Task<ReviewDto?> Handle(GetReviewByIdQuery request, CancellationToken cancellationToken)
    {
        return _queries.GetByIdAsync(request.Id, cancellationToken);
    }
}

