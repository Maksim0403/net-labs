namespace Fleet.Bll.Dto;

/// <summary>Параметри пагінації, фільтрації та сортування списку кур'єрів.</summary>
public class CourierQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public long? WarehouseId { get; set; }
    public string? ZoneName { get; set; }
    public string SortBy { get; set; } = "name";   // name | createdAt
    public string SortDir { get; set; } = "asc";   // asc | desc
}

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNext => Page < TotalPages;
    public bool HasPrevious => Page > 1;
}
