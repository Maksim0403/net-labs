using Shipping.Domain.Models;

namespace Shipping.Dal.Abstractions;

public interface IServiceTypeRepository : IGenericRepository<ServiceType>
{
    Task<ServiceType?> GetByCodeAsync(string code, CancellationToken ct = default);
}
