using System.Diagnostics;
using System.Net.Http.Json;
using Platform.Aggregator.DTOs;

namespace Platform.Aggregator.Clients;

public class ShippingClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShippingClient> _logger;

    public ShippingClient(HttpClient httpClient, ILogger<ShippingClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(AggregatedShipmentDto? Data, string? Warning)> GetShipmentByIdAsync(long id, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"/api/shipments/{id}", ct);
            sw.Stop();
            _logger.LogInformation("ShippingClient: GET /api/shipments/{Id} finished with status {Status} in {DurationMs}ms",
                id, response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"Shipping service returned status {(int)response.StatusCode} ({response.ReasonPhrase}) for shipment {id}");
            }

            var dto = await response.Content.ReadFromJsonAsync<ShippingApiResponse>(cancellationToken: ct);
            if (dto is null)
            {
                return (null, $"Shipping service returned empty body for shipment {id}");
            }

            return (new AggregatedShipmentDto
            {
                Id = dto.Id,
                CustomerId = dto.CustomerId,
                TrackingNumber = dto.TrackingNumber,
                Status = dto.Status,
                TotalPrice = dto.TotalPrice,
                SenderAddress = dto.Details?.SenderAddress ?? dto.SenderAddress,
                RecipientName = dto.Details?.RecipientName ?? dto.RecipientName,
                RecipientAddress = dto.Details?.RecipientAddress ?? dto.RecipientAddress,
                WeightKg = dto.Details?.WeightKg ?? dto.WeightKg,
                DeclaredValue = dto.Details?.DeclaredValue ?? dto.DeclaredValue,
                CreatedAt = dto.CreatedAt,
                ConfirmedAt = dto.ConfirmedAt
            }, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "ShippingClient: Failed to fetch shipment {Id} in {DurationMs}ms", id, sw.ElapsedMilliseconds);
            return (null, $"Shipping service unavailable: {ex.Message}");
        }
    }
}
