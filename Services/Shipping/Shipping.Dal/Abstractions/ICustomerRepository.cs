using Shipping.Domain.Models;

namespace Shipping.Dal.Abstractions;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    /// <summary>Оновлює дату і користувача останньої модифікації клієнта.</summary>
    Task TouchAsync(long customerId, string updatedBy, CancellationToken ct = default);
}

