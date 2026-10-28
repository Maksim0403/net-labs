using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
[Route("api/fleet/warehouses")]
public class WarehousesController(IWarehouseService service) : ControllerBase
{
    [HttpGet("{id:long}")]
    public async Task<ActionResult<WarehouseWithVehiclesDto>> GetById(long id, CancellationToken ct)
        => Ok(await service.GetWithVehiclesAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<WarehouseDto>> Create(
        CreateWarehouseDto input, [FromServices] IValidator<CreateWarehouseDto> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(input, ct);
        if (!result.IsValid)
            return ValidationProblem(new ValidationProblemDetails(result.ToDictionary()));

        var created = await service.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
