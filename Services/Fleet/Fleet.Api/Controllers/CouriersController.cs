using Fleet.Api.Metrics;
using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

[ApiController]
[Route("api/couriers")]
[Route("api/fleet/couriers")]
public class CouriersController(ICourierService service, FleetMetrics metrics) : ControllerBase
{
    /// <summary>Список з пагінацією, фільтрами (search, warehouseId, zoneName) і сортуванням (sortBy, sortDir).</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<CourierDto>>> GetList([FromQuery] CourierQuery query, CancellationToken ct)
    {
        var result = await service.GetListAsync(query, ct);
        Response.Headers["X-Total-Count"] = result.TotalCount.ToString();
        Response.Headers["X-Page"] = result.Page.ToString();
        Response.Headers["X-Page-Size"] = result.PageSize.ToString();
        return Ok(result);
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CourierDetailsDto>> GetById(long id, CancellationToken ct)
    {
        // Вимірювання тривалості операції для успішних і невдалих сценаріїв (Частина 2.3, Критерій 7)
        var sw = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            var result = await service.GetByIdAsync(id, ct);
            metrics.CourierViewed("default");
            metrics.RecordDuration("get_courier", success: true, sw.Elapsed.TotalMilliseconds);
            return Ok(result);
        }
        catch
        {
            metrics.RecordDuration("get_courier", success: false, sw.Elapsed.TotalMilliseconds);
            throw;
        }
    }

    [HttpPost]
    public async Task<ActionResult<CourierDto>> Create(
        CreateCourierDto input, [FromServices] IValidator<CreateCourierDto> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        if (!result.IsValid)
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/zones")]
    public async Task<IActionResult> AssignToZone(long id, AssignZoneDto input, CancellationToken ct)
    {
        await service.AssignToZoneAsync(id, input.ZoneId, ct);
        return NoContent();
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<CourierDto>> Update(long id, UpdateCourierDto input, CancellationToken ct)
    {
        var updated = await service.UpdateAsync(id, input, ct);
        return Ok(updated);
    }
}
