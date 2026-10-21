using System.Data;
using AutoMapper;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Dal.Abstractions;
using Shipping.Domain.Exceptions;
using Shipping.Domain.Models;

namespace Shipping.Bll.Services;

public sealed class ShipmentService : IShipmentService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public ShipmentService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow;
        _mapper = mapper;
    }

    public async Task<ShipmentDto?> GetByIdAsync(long id, CancellationToken ct = default)
    {
        var shipment = await _uow.Shipments.GetWithItemsAsync(id, ct);
        return shipment is null ? null : _mapper.Map<ShipmentDto>(shipment);
    }

    public async Task<IReadOnlyList<ShipmentDto>> GetCustomerShipmentsAsync(
        long customerId, byte? status, int page, int pageSize, CancellationToken ct = default)
    {
        var shipments = await _uow.Shipments.GetPageAsync(customerId, status, page, pageSize, ct);
        return shipments.Select(_mapper.Map<ShipmentDto>).ToList();
    }

    /// <summary>
    /// Створення відправлення. Назву й ціну послуги беремо зі знімка, який
    /// формує сама процедура usp_CreateShipment на боці БД (Лаб. 01,
    /// docs/dedup-policy.md) — тут лише перевіряємо, що клієнт існує.
    /// </summary>
    public async Task<ShipmentDto> CreateAsync(CreateShipmentDto input, CancellationToken ct = default)
    {
        var customer = await _uow.Customers.GetByIdAsync(input.CustomerId, ct)
            ?? throw new NotFoundException($"Клієнта {input.CustomerId} не знайдено");

        var shipment = new Shipment
        {
            CustomerId = customer.Id,
            Details = new ShipmentDetails
            {
                SenderAddress = input.SenderAddress,
                RecipientName = input.RecipientName,
                RecipientPhone = input.RecipientPhone,
                RecipientAddress = input.RecipientAddress,
                WeightKg = input.WeightKg,
                DeclaredValue = input.DeclaredValue
            },
            Items = input.Items
                .Select(i => new Shipping.Domain.Models.ShipmentService { 
                    ServiceTypeId = i.ServiceTypeId,
                    Quantity = i.Quantity 
                })
                .ToList()
        };

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Shipments.AddAsync(shipment, ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        var created = await _uow.Shipments.GetWithItemsAsync(shipment.Id, ct);
        return _mapper.Map<ShipmentDto>(created);
    }

    /// <summary>
    /// Крок C.2 — флагманська транзакційна операція: зміна статусу
    /// відправлення (через процедуру з Лаб. 01) + оновлення запису клієнта,
    /// координовані Unit of Work через ДВА різні репозиторії (Shipments і
    /// Customers) в одній SQL-транзакції. Якщо другий крок кине виняток —
    /// RollbackAsync поверне статус відправлення назад (контрольна точка
    /// методички: "операція при штучній помилці в другому репозиторії
    /// відкочує зміни першого").
    /// </summary>
    public async Task ConfirmAsync(long shipmentId, CancellationToken ct = default)
    {
        var shipment = await _uow.Shipments.GetWithItemsAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Відправлення {shipmentId} не знайдено");

        if (shipment.Items.Count == 0)
            throw new ValidationException("У відправленні немає жодної послуги");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Репозиторій №1: Shipments — зміна статусу через процедуру БД
            await _uow.Shipments.ConfirmAsync(shipmentId, "shipping-api", ct);

            // Репозиторій №2: Customers — оновлення аудитних полів клієнта
            // (в одній транзакції з кроком вище)
            await _uow.Customers.TouchAsync(shipment.CustomerId, "shipping-api", ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task ChangeStatusAsync(long shipmentId, string newStatus, CancellationToken ct = default)
    {
        if (!Enum.TryParse<ShipmentStatus>(newStatus, ignoreCase: true, out var status))
            throw new ValidationException($"Невідомий статус: {newStatus}");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Shipments.ChangeStatusAsync(shipmentId, status, "shipping-api", ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task CancelAsync(long shipmentId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Shipments.CancelAsync(shipmentId, "shipping-api", ct);
            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
