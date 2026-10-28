using System.Diagnostics;
using System.Net.Http.Json;
using Platform.Aggregator.DTOs;

namespace Platform.Aggregator.Clients;

public class ReviewsClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReviewsClient> _logger;

    public ReviewsClient(HttpClient httpClient, ILogger<ReviewsClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(IReadOnlyList<AggregatedReviewDto>? Data, string? Warning)> GetReviewsByShipmentIdAsync(long shipmentId, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"/api/reviews?shipmentId={shipmentId}", ct);
            sw.Stop();
            _logger.LogInformation("ReviewsClient: GET /api/reviews?shipmentId={ShipmentId} finished with status {Status} in {DurationMs}ms",
                shipmentId, response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"Reviews service returned status {(int)response.StatusCode} ({response.ReasonPhrase}) for shipment {shipmentId}");
            }

            var pagedResult = await response.Content.ReadFromJsonAsync<ReviewsApiResponse>(cancellationToken: ct);
            var items = pagedResult?.Items?.Select(r => new AggregatedReviewDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = r.CreatedAt
            }).ToList() ?? new List<AggregatedReviewDto>();

            return (items, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "ReviewsClient: Failed to fetch reviews for shipment {ShipmentId} in {DurationMs}ms", shipmentId, sw.ElapsedMilliseconds);
            return (null, $"Reviews service unavailable: {ex.Message}");
        }
    }
}

