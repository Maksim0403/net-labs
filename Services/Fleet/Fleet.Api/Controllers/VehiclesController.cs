using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

[ApiController]
[Route("api/vehicles")]
[Route("api/fleet/vehicles")]
public class VehiclesController(IVehicleService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<VehicleDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetByIdAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<VehicleDto>> Create(
        CreateVehicleDto input, [FromServices] IValidator<CreateVehicleDto> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        if (!result.IsValid)
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }

    [HttpPut("{id:long}/capacity")]
    public async Task<ActionResult<VehicleDto>> ChangeCapacity(long id, ChangeCapacityDto input, CancellationToken ct)
        => Ok(await service.ChangeCapacityAsync(id, input.CapacityKg, ct));
}
