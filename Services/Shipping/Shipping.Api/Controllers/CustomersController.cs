using Microsoft.AspNetCore.Mvc;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;

namespace Shipping.Api.Controllers;

[ApiController]
[Route("api/customers")]
[Route("api/shipping/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;

    public CustomersController(ICustomerService customers) => _customers = customers;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerDto>>> GetAll(CancellationToken ct)
        => Ok(await _customers.GetAllAsync(ct));

    [HttpGet("{id:long}")]
    public async Task<ActionResult<CustomerDto>> GetById(long id, CancellationToken ct)
    {
        var customer = await _customers.GetByIdAsync(id, ct);
        return customer is null ? NotFound() : Ok(customer);
    }

    [HttpPost]
    public async Task<ActionResult<CustomerDto>> Create(CreateCustomerDto input, CancellationToken ct)
    {
        var created = await _customers.CreateAsync(input, ct);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
    }
}
