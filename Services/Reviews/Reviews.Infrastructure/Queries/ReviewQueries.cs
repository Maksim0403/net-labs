using MongoDB.Bson;
using MongoDB.Driver;
using Reviews.Application.Common;
using Reviews.Application.DTOs;
using Reviews.Application.Queries;
using Reviews.Domain.Entities;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Queries;

/// <summary>Реалізація оптимізованих запитів читання з пагінацією та проєкцією безпосередньо на боці MongoDB.</summary>
public sealed class ReviewQueries : IReviewQueries
{
    private readonly MongoDbContext _context;

    public ReviewQueries(MongoDbContext context)
    {
        _context = context;
    }

    private static readonly ProjectionDefinition<Review, ReviewDto> DtoProjection =
        Builders<Review>.Projection.Expression(r => new ReviewDto
        {
            Id = r.Id.ToString(),
            ShipmentId = r.ShipmentId,
            CustomerId = r.CustomerId,
            CourierId = r.CourierId,
            CourierName = r.CourierName,
            Type = r.Type,
            Rating = r.Rating.Value,
            Text = r.Text,
            Criteria = r.Criteria != null ? new ReviewCriteriaDto
            {
                Speed = r.Criteria.Speed,
                Politeness = r.Criteria.Politeness,
                Packaging = r.Criteria.Packaging
            } : null,
            Comments = r.Comments.Select(c => new ReviewCommentDto
            {
                Author = c.Author,
                Text = c.Text,
                CreatedAt = c.CreatedAt
            }).ToList(),
            Version = r.Version,
            CreatedAt = r.CreatedAt,
            UpdatedAt = r.UpdatedAt
        });

    public async Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(id, out var objectId))
            return null;

        var filter = Builders<Review>.Filter.And(
            Builders<Review>.Filter.Eq(r => r.Id, objectId),
            Builders<Review>.Filter.Eq(r => r.IsDeleted, false)
        );

        return await _context.Reviews.Find(filter)
            .Project(DtoProjection)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<PagedResult<ReviewDto>> GetListAsync(GetReviewsQuery query, CancellationToken ct = default)
    {
        var filterBuilder = Builders<Review>.Filter;
        var filterList = new List<FilterDefinition<Review>>
        {
            filterBuilder.Eq(r => r.IsDeleted, false)
        };

        if (query.CourierId.HasValue)
            filterList.Add(filterBuilder.Eq(r => r.CourierId, query.CourierId.Value));

        if (query.CustomerId.HasValue)
            filterList.Add(filterBuilder.Eq(r => r.CustomerId, query.CustomerId.Value));

        if (query.ShipmentId.HasValue)
            filterList.Add(filterBuilder.Eq(r => r.ShipmentId, query.ShipmentId.Value));

        if (query.MinRating.HasValue)
            filterList.Add(filterBuilder.Gte(r => r.Rating.Value, query.MinRating.Value));

        if (!string.IsNullOrWhiteSpace(query.SearchText))
            filterList.Add(filterBuilder.Text(query.SearchText));

        var combinedFilter = filterBuilder.And(filterList);

        var totalCount = await _context.Reviews.CountDocumentsAsync(combinedFilter, cancellationToken: ct);

        // Пагінація та проєкція безпосередньо в MongoDB через Project, Skip і Limit
        var items = await _context.Reviews.Find(combinedFilter)
            .Project(DtoProjection)
            .SortByDescending(r => r.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Limit(query.PageSize)
            .ToListAsync(ct);

        return new PagedResult<ReviewDto>(items, query.Page, query.PageSize, totalCount);
    }
}

