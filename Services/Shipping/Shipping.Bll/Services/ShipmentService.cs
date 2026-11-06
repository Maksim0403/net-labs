using System.Data;
using System.Diagnostics;
using AutoMapper;
using Microsoft.Extensions.Logging;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Dal.Abstractions;
using Shipping.Domain.Exceptions;
using Shipping.Domain.Models;

namespace Shipping.Bll.Services;

public sealed class ShipmentService : IShipmentService
{
    private static readonly ActivitySource ActivitySource = new("Platform.Diagnostics", "1.0.0");
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;
    private readonly ILogger<ShipmentService>? _logger;

    public ShipmentService(IUnitOfWork uow, IMapper mapper, ILogger<ShipmentService>? logger = null)
    {
        _uow = uow;
        _mapper = mapper;
        _logger = logger;
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

    /// <summary>Створення відправлення з перевіркою існування клієнта.</summary>
    public async Task<ShipmentDto> CreateAsync(CreateShipmentDto input, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Shipping.CreateShipment");
        activity?.SetTag("shipping.customer_id", input.CustomerId);
        activity?.SetTag("shipping.items_count", input.Items.Count);

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

        activity?.SetTag("shipping.shipment_id", shipment.Id);
        activity?.SetTag("shipping.status", shipment.Status.ToString());
        _logger?.LogInformation("Shipment created successfully. ShipmentId: {ShipmentId}, CustomerId: {CustomerId}, Status: {Status}",
            shipment.Id, input.CustomerId, shipment.Status);

        var created = await _uow.Shipments.GetWithItemsAsync(shipment.Id, ct);
        return _mapper.Map<ShipmentDto>(created);
    }

    /// <summary>Підтвердження відправлення та оновлення клієнта в одній транзакції UoW.</summary>
    public async Task ConfirmAsync(long shipmentId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Shipping.ConfirmShipment");
        activity?.SetTag("shipping.shipment_id", shipmentId);

        var shipment = await _uow.Shipments.GetWithItemsAsync(shipmentId, ct)
            ?? throw new NotFoundException($"Відправлення {shipmentId} не знайдено");

        if (shipment.Items.Count == 0)
            throw new ValidationException("У відправленні немає жодної послуги");

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            await _uow.Shipments.ConfirmAsync(shipmentId, "shipping-api", ct);
            await _uow.Customers.TouchAsync(shipment.CustomerId, "shipping-api", ct);

            await _uow.CommitAsync(ct);
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        activity?.SetTag("shipping.customer_id", shipment.CustomerId);
        activity?.SetTag("shipping.status", "Confirmed");
        _logger?.LogInformation("Shipment confirmed successfully. ShipmentId: {ShipmentId}, CustomerId: {CustomerId}, Status: {Status}",
            shipmentId, shipment.CustomerId, "Confirmed");
    }

    public async Task ChangeStatusAsync(long shipmentId, string newStatus, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Shipping.ChangeStatus");
        activity?.SetTag("shipping.shipment_id", shipmentId);
        activity?.SetTag("shipping.status", newStatus);

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

        _logger?.LogInformation("Shipment status updated. ShipmentId: {ShipmentId}, Status: {Status}", shipmentId, newStatus);
    }

    public async Task CancelAsync(long shipmentId, CancellationToken ct = default)
    {
        using var activity = ActivitySource.StartActivity("Shipping.CancelShipment");
        activity?.SetTag("shipping.shipment_id", shipmentId);

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

        _logger?.LogInformation("Shipment cancelled. ShipmentId: {ShipmentId}, Status: {Status}", shipmentId, "Cancelled");
    }
}
