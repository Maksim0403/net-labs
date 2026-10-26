using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

[ApiController]
[Route("api/zones")]
public class ZonesController(ICourierService couriers) : ControllerBase
{
    /// <summary>Кур'єри, призначені в зону не раніше дати since (за замовчуванням останні 30 днів). M:N через проміжну сутність.</summary>
    [HttpGet("{zoneName}/couriers")]
    public async Task<ActionResult<IReadOnlyList<CourierDto>>> GetCouriers(
        string zoneName, [FromQuery] DateTime? since, CancellationToken ct)
        => Ok(await couriers.GetAssignedToZoneSinceAsync(zoneName, since ?? DateTime.UtcNow.AddDays(-30), ct));
}
