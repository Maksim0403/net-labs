using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

[ApiController]
[Route("api/routes")]
[Route("api/fleet/routes")]
public class RoutesController(IRouteService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<RouteDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<RouteDto>> Create(
        CreateRouteDto input, [FromServices] IValidator<CreateRouteDto> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        if (!result.IsValid)
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));

        var created = await service.PlanAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPost("{id:long}/start")]
    public async Task<IActionResult> Start(long id, CancellationToken ct)
    {
        await service.StartAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/complete")]
    public async Task<IActionResult> Complete(long id, CancellationToken ct)
    {
        await service.CompleteAsync(id, ct);
        return NoContent();
    }

    [HttpPost("{id:long}/cancel")]
    public async Task<IActionResult> Cancel(long id, CancellationToken ct)
    {
        await service.CancelAsync(id, ct);
        return NoContent();
    }
}
