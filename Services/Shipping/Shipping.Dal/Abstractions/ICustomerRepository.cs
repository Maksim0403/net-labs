using Shipping.Domain.Models;

namespace Shipping.Dal.Abstractions;

public interface ICustomerRepository : IGenericRepository<Customer>
{
    Task<Customer?> GetByPhoneAsync(string phone, CancellationToken ct = default);

    /// <summary>
    /// Оновлює UpdatedAt/UpdatedBy клієнта. Використовується як другий репозиторій
    /// у транзакційному сценарії ConfirmShipmentAsync (Крок C.2), щоб продемонструвати
    /// координацію кількох репозиторіїв в одній транзакції Unit of Work.
    /// </summary>
    Task TouchAsync(long customerId, string updatedBy, CancellationToken ct = default);
}
