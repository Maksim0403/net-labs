using Shipping.Bll.Dto;

namespace Shipping.Bll.Abstractions;

public interface IShipmentService
{
    Task<ShipmentDto?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<IReadOnlyList<ShipmentDto>> GetCustomerShipmentsAsync(long customerId, byte? status, int page, int pageSize, CancellationToken ct = default);
    Task<ShipmentDto> CreateAsync(CreateShipmentDto input, CancellationToken ct = default);

    /// <summary>
    /// Транзакційна бізнес-операція (Крок C.2): підтвердження відправлення
    /// (usp_ConfirmShipment) + оновлення клієнта (TouchAsync) в одній
    /// транзакції UoW через два репозиторії. При помилці в другому кроці —
    /// повний відкат, включно зі зміною статусу першого.
    /// </summary>
    Task ConfirmAsync(long shipmentId, CancellationToken ct = default);

    Task ChangeStatusAsync(long shipmentId, string newStatus, CancellationToken ct = default);
    Task CancelAsync(long shipmentId, CancellationToken ct = default);
}
