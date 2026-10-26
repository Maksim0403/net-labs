using Reviews.Application.Common;
using Reviews.Application.DTOs;

namespace Reviews.Application.Queries;

/// <summary>Контракт оптимізованих запитів читання відгуків напряму з MongoDB.</summary>
public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<ReviewDto>> GetListAsync(GetReviewsQuery query, CancellationToken ct = default);
}

