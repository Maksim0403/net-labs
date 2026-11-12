using Fleet.Bll.Services;
using Microsoft.AspNetCore.Mvc;

namespace Fleet.Api.Controllers;

public record ChangePriceRequest(decimal NewPrice);
public record RenameProductRequest(string NewName);

[ApiController]
[Route("api/[controller]")]
public class ProductsController(ICatalogService catalogService) : ControllerBase
{
    [HttpPut("{id:int}/price")]
    public async Task<IActionResult> ChangePrice(int id, [FromBody] ChangePriceRequest request, CancellationToken ct)
    {
        await catalogService.ChangePriceAsync(id, request.NewPrice, ct);
        return Ok(new { ProductId = id, request.NewPrice, Message = "Price updated and event published" });
    }

    [HttpPut("{id:int}/rename")]
    public async Task<IActionResult> Rename(int id, [FromBody] RenameProductRequest request, CancellationToken ct)
    {
        await catalogService.RenameProductAsync(id, request.NewName, ct);
        return Ok(new { ProductId = id, request.NewName, Message = "Product renamed and event published" });
    }
}

