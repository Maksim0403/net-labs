using Shipping.Bll.Dto;

namespace Shipping.Bll.Abstractions;

public interface ICustomerService
{
    Task<CustomerDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerDto>> GetAllAsync(CancellationToken ct = default);
    Task<CustomerDto> CreateAsync(CreateCustomerDto input, CancellationToken ct = default);
}
