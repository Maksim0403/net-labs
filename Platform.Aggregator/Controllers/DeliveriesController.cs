using System.Diagnostics;
using System.Text.Json;
using Fleet.Grpc;
using Grpc.Core;
using GrpcStatusCode = Grpc.Core.StatusCode;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Platform.Aggregator.DTOs;
using Reviews.Grpc;
using Shipping.Grpc;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/deliveries")]
[Produces("application/json")]
public class DeliveriesController : ControllerBase
{
    private readonly ShippingService.ShippingServiceClient _shippingClient;
    private readonly FleetService.FleetServiceClient _fleetClient;
    private readonly ReviewsService.ReviewsServiceClient _reviewsClient;
    private readonly IDistributedCache _cache;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(
        ShippingService.ShippingServiceClient shippingClient,
        FleetService.FleetServiceClient fleetClient,
        ReviewsService.ReviewsServiceClient reviewsClient,
        IDistributedCache cache,
        ILogger<DeliveriesController> logger)
    {
        _shippingClient = shippingClient;
        _fleetClient = fleetClient;
        _reviewsClient = reviewsClient;
        _cache = cache;
        _logger = logger;
    }

    /// <summary>
    /// Агрегований огляд доставки через gRPC: об'єднує дані Shipping, Fleet та Reviews.
    /// Перевіряє кеш Redis перед gRPC-викликами з коротким TTL (30 с).
    /// Незалежні gRPC-запити виконуються паралельно через Task.WhenAll.
    /// У разі часткових відмов (RpcException: Unavailable/DeadlineExceeded) повертає HTTP 200 OK з Warnings.
    /// Часткові відповіді не кешуються.
    /// </summary>
    [HttpGet("{id:long}/overview")]
    [ProducesResponseType(typeof(ShipmentOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShipmentOverviewDto>> GetOverview(
        [FromRoute] long id,
        [FromQuery] long? courierId,
        CancellationToken ct)
    {
        var targetCourierId = courierId ?? 1;
        var cacheKey = $"aggregator:delivery:{id}:{targetCourierId}:full";

        // Перевірка розподіленого кешу ДО паралельних gRPC-викликів (Graceful degradation кешу Redis - Критерій 5)
        byte[]? cachedBytes = null;
        try
        {
            cachedBytes = await _cache.GetAsync(cacheKey, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis unavailable, falling back to database for {Key}", cacheKey);
        }

        if (cachedBytes is not null)
        {
            _logger.LogInformation("Aggregator cache hit {Key}", cacheKey);
            var cached = JsonSerializer.Deserialize<ShipmentOverviewDto>(cachedBytes);
            if (cached is not null)
            {
                return Ok(cached);
            }
        }

        _logger.LogInformation("Aggregator cache miss {Key}", cacheKey);
        var totalSw = Stopwatch.StartNew();
        _logger.LogInformation("Starting delivery overview gRPC aggregation for ShipmentId: {ShipmentId}", id);

        var warnings = new List<string>();

        // Паралельні gRPC-виклики з CancellationToken
        var shipmentTask = SafeGrpcAsync(
            () => _shippingClient.GetShipmentAsync(new GetShipmentRequest { Id = id }, cancellationToken: ct).ResponseAsync,
            "shipping-api",
            warnings);

        var courierTask = SafeGrpcAsync(
            () => _fleetClient.GetCourierAsync(new GetCourierRequest { Id = targetCourierId }, cancellationToken: ct).ResponseAsync,
            "fleet-api",
            warnings);

        var reviewsTask = SafeGrpcAsync(
            () => _reviewsClient.GetReviewsByShipmentAsync(new GetReviewsByShipmentRequest { ShipmentId = id }, cancellationToken: ct).ResponseAsync,
            "reviews-api",
            warnings);

        await Task.WhenAll(shipmentTask, courierTask, reviewsTask);

        var shipmentReply = await shipmentTask;
        var courierReply = await courierTask;
        var reviewsReply = await reviewsTask;

        totalSw.Stop();
        _logger.LogInformation(
            "Completed delivery overview gRPC aggregation for ShipmentId: {ShipmentId} in {TotalDurationMs}ms with {WarningCount} warnings",
            id, totalSw.ElapsedMilliseconds, warnings.Count);

        var overview = new ShipmentOverviewDto
        {
            ShipmentId = id,
            Status = shipmentReply?.Status ?? "Unknown",
            Shipment = shipmentReply is null ? null : new AggregatedShipmentDto
            {
                Id = shipmentReply.Id,
                CustomerId = shipmentReply.CustomerId,
                TrackingNumber = shipmentReply.TrackingNumber,
                Status = shipmentReply.Status,
                TotalPrice = (decimal)shipmentReply.TotalPrice,
                SenderAddress = shipmentReply.SenderAddress,
                RecipientName = shipmentReply.RecipientName,
                RecipientAddress = shipmentReply.RecipientAddress,
                WeightKg = (decimal)shipmentReply.WeightKg,
                DeclaredValue = (decimal)shipmentReply.DeclaredValue,
                CreatedAt = DateTime.TryParse(shipmentReply.CreatedAt, out var ca) ? ca : null,
                ConfirmedAt = DateTime.TryParse(shipmentReply.ConfirmedAt, out var cfa) ? cfa : null
            },
            Courier = courierReply is null ? null : new AggregatedCourierDto
            {
                Id = courierReply.Id,
                FullName = courierReply.FullName,
                Phone = courierReply.Phone,
                Status = courierReply.Status,
                WarehouseCode = courierReply.WarehouseCode,
                WarehouseName = courierReply.WarehouseName
            },
            Reviews = reviewsReply?.Items.Select(r => new AggregatedReviewDto
            {
                Id = r.Id,
                CustomerId = r.CustomerId,
                Rating = r.Rating,
                Text = r.Text,
                CreatedAt = DateTime.TryParse(r.CreatedAt, out var rca) ? rca : DateTime.UtcNow
            }).ToList(),
            Warnings = warnings
        };

        // Кешування агрегованої відповіді в Redis з коротким TTL (30 с)
        // Часткові відповіді не кешуємо
        if (overview.Warnings.Count == 0)
        {
            try
            {
                await _cache.SetAsync(
                    cacheKey,
                    JsonSerializer.SerializeToUtf8Bytes(overview),
                    new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(30) },
                    ct);
                _logger.LogInformation("Aggregated response cached in Redis {Key} (TTL 30s)", cacheKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Redis unavailable, value for {Key} not cached", cacheKey);
            }
        }

        return Ok(overview);
    }

    private async Task<T?> SafeGrpcAsync<T>(
        Func<Task<T>> call,
        string serviceName,
        List<string> warnings) where T : class
    {
        try
        {
            return await call();
        }
        catch (RpcException ex) when (ex.StatusCode is GrpcStatusCode.Unavailable or GrpcStatusCode.DeadlineExceeded or GrpcStatusCode.NotFound)
        {
            _logger.LogWarning(ex, "gRPC service {Service} returned status {Status}", serviceName, ex.StatusCode);
            warnings.Add($"{serviceName} unavailable: {ex.Status.Detail ?? ex.StatusCode.ToString()}");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "gRPC call to {Service} failed: {Message}", serviceName, ex.Message);
            warnings.Add($"{serviceName} call failed: {ex.Message}");
            return null;
        }
    }
}
