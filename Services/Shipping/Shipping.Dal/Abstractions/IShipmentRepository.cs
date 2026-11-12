using Shipping.Domain.Models;

namespace Shipping.Dal.Abstractions;

public interface IShipmentRepository : IGenericRepository<Shipment>
{
    /// <summary>Шапка + деталі (1:1) + позиції (M:N), одним multi-mapping запитом.</summary>
    Task<Shipment?> GetWithItemsAsync(long shipmentId, CancellationToken ct = default);

    /// <summary>Пагінований список відправлень клієнта (dbo.usp_GetCustomerShipments).</summary>
    Task<IReadOnlyList<Shipment>> GetPageAsync(long customerId, byte? status, int page, int pageSize, CancellationToken ct = default);

    /// <summary>Підтвердження (dbo.usp_ConfirmShipment, Draft -&gt; Confirmed).</summary>
    Task ConfirmAsync(long shipmentId, string confirmedBy, CancellationToken ct = default);

    /// <summary>Транзакційна зміна статусу (dbo.usp_ChangeShipmentStatus).</summary>
    Task ChangeStatusAsync(long shipmentId, ShipmentStatus newStatus, string changedBy, CancellationToken ct = default);

    /// <summary>Скасування (dbo.usp_CancelShipment).</summary>
    Task CancelAsync(long shipmentId, string changedBy, CancellationToken ct = default);

    /// <summary>Оновлення денормалізованого снепшоту кур'єра при отриманні події CourierUpdated (Лаб. 08, Критерій 10).</summary>
    Task UpdateCourierSnapshotAsync(long courierId, string fullName, string phone, CancellationToken ct = default);
}
