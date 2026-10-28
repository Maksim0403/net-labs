using System.Diagnostics;
using System.Net.Http.Json;
using Platform.Aggregator.DTOs;

namespace Platform.Aggregator.Clients;

public class FleetClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<FleetClient> _logger;

    public FleetClient(HttpClient httpClient, ILogger<FleetClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<(AggregatedCourierDto? Data, string? Warning)> GetCourierByIdAsync(long id, CancellationToken ct)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await _httpClient.GetAsync($"/api/couriers/{id}", ct);
            sw.Stop();
            _logger.LogInformation("FleetClient: GET /api/couriers/{Id} finished with status {Status} in {DurationMs}ms",
                id, response.StatusCode, sw.ElapsedMilliseconds);

            if (!response.IsSuccessStatusCode)
            {
                return (null, $"Fleet service returned status {(int)response.StatusCode} ({response.ReasonPhrase}) for courier {id}");
            }

            var dto = await response.Content.ReadFromJsonAsync<CourierApiResponse>(cancellationToken: ct);
            if (dto is null)
            {
                return (null, $"Fleet service returned empty body for courier {id}");
            }

            return (new AggregatedCourierDto
            {
                Id = dto.Id,
                FullName = dto.FullName,
                Phone = dto.Phone,
                Status = dto.Status,
                WarehouseCode = dto.WarehouseCode,
                WarehouseName = dto.Warehouse?.Name ?? dto.WarehouseCode
            }, null);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogWarning(ex, "FleetClient: Failed to fetch courier {Id} in {DurationMs}ms", id, sw.ElapsedMilliseconds);
            return (null, $"Fleet service unavailable: {ex.Message}");
        }
    }
}
