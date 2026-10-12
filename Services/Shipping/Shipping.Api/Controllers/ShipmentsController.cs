using Microsoft.AspNetCore.Mvc;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;

namespace Shipping.Api.Controllers;

/// <summary>Тонкий контролер (Крок D): без SQL і бізнес-правил, лише делегування в BLL.</summary>
[ApiController]
[Route("api/shipments")]
public class ShipmentsController : ControllerBase
{
    private readonly IShipmentService _shipments;

    public ShipmentsController(IShipmentService shipments) => _shipments = shipments;

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ShipmentDto>> GetById(long id, CancellationToken ct)
    {
        var shipment = await _shipments.GetByIdAsync(id, ct);
        return shipment is null ? NotFound() : Ok(shipment);
    }

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ShipmentDto>>> GetByCustomer(
        [FromQuery] long customerId, [FromQuery] byte? status,
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
        => Ok(await _shipments.GetCustomerShipmentsAsync(customerId, status, page, pageSize, ct));

    [HttpPost]
    public async Task<ActionResult<ShipmentDto>> Create(CreateShipmentDto input, CancellationToken ct)
    {
        var created = await _shipments.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    /// <summary>Транзакційна бізнес-операція (Крок C.2): 204, або 404/409 через глобальний обробник.</summary>
    [HttpPost("{id:long}/confirm")]
    public async Task<IActionResult> Confirm(long id, CancellationToken ct)
    {
        await _shipments.ConfirmAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/status")]
    public async Task<IActionResult> ChangeStatus(long id, [FromBody] string newStatus, CancellationToken ct)
    {
        await _shipments.ChangeStatusAsync(id, newStatus, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await _shipments.CancelAsync(id, ct);
        return NoContent();
    }
}
