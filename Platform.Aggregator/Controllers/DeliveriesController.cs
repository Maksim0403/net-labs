using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Clients;
using Platform.Aggregator.DTOs;

namespace Platform.Aggregator.Controllers;

[ApiController]
[Route("api/deliveries")]
[Produces("application/json")]
public class DeliveriesController : ControllerBase
{
    private readonly ShippingClient _shippingClient;
    private readonly FleetClient _fleetClient;
    private readonly ReviewsClient _reviewsClient;
    private readonly ILogger<DeliveriesController> _logger;

    public DeliveriesController(
        ShippingClient shippingClient,
        FleetClient fleetClient,
        ReviewsClient reviewsClient,
        ILogger<DeliveriesController> logger)
    {
        _shippingClient = shippingClient;
        _fleetClient = fleetClient;
        _reviewsClient = reviewsClient;
        _logger = logger;
    }

    /// <summary>
    /// Агрегований огляд доставки: об'єднує дані Shipping, Fleet та Reviews у єдиний композитний DTO.
    /// Незалежні запити виконуються паралельно через Task.WhenAll.
    /// У разі часткових відмов повертає HTTP 200 OK з доступними даними та масивом Warnings.
    /// </summary>
    [HttpGet("{id:long}/overview")]
    [ProducesResponseType(typeof(ShipmentOverviewDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<ShipmentOverviewDto>> GetOverview(
        [FromRoute] long id,
        [FromQuery] long? courierId,
        CancellationToken ct)
    {
        var totalSw = Stopwatch.StartNew();
        _logger.LogInformation("Starting delivery overview aggregation for ShipmentId: {ShipmentId}", id);

        var shipmentTask = _shippingClient.GetShipmentByIdAsync(id, ct);
        var courierTask = _fleetClient.GetCourierByIdAsync(courierId ?? 1, ct);
        var reviewsTask = _reviewsClient.GetReviewsByShipmentIdAsync(id, ct);

        await Task.WhenAll(shipmentTask, courierTask, reviewsTask);

        var (shipment, shipmentWarning) = await shipmentTask;
        var (courier, courierWarning) = await courierTask;
        var (reviews, reviewsWarning) = await reviewsTask;

        var warnings = new List<string>();
        if (shipmentWarning is not null) warnings.Add(shipmentWarning);
        if (courierWarning is not null) warnings.Add(courierWarning);
        if (reviewsWarning is not null) warnings.Add(reviewsWarning);

        totalSw.Stop();
        _logger.LogInformation(
            "Completed delivery overview aggregation for ShipmentId: {ShipmentId} in {TotalDurationMs}ms with {WarningCount} warnings",
            id, totalSw.ElapsedMilliseconds, warnings.Count);

        var overview = new ShipmentOverviewDto
        {
            ShipmentId = id,
            Status = shipment?.Status ?? "Unknown",
            Shipment = shipment,
            Courier = courier,
            Reviews = reviews,
            Warnings = warnings
        };

        return Ok(overview);
    }
}

