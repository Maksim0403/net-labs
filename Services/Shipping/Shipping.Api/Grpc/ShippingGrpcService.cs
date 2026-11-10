using Grpc.Core;
using Shipping.Bll.Abstractions;
using Shipping.Bll.Dto;
using Shipping.Grpc;

namespace Shipping.Api.Grpc;

public class ShippingGrpcService(IShipmentService shipments) : ShippingService.ShippingServiceBase
{
    public override async Task<ShipmentReply> GetShipment(GetShipmentRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

        var ct = context?.CancellationToken ?? CancellationToken.None;
        var dto = await shipments.GetByIdAsync(request.Id, ct);
        if (dto is null)
            throw new RpcException(new Status(StatusCode.NotFound, $"Shipment {request.Id} not found"));

        return new ShipmentReply
        {
            Id = dto.Id,
            CustomerId = dto.CustomerId,
            TrackingNumber = dto.TrackingNumber,
            Status = dto.Status,
            TotalPrice = (double)dto.TotalPrice,
            RecipientName = dto.RecipientName ?? string.Empty,
            RecipientAddress = dto.RecipientAddress ?? string.Empty,
            SenderAddress = string.Empty,
            RecipientPhone = string.Empty,
            WeightKg = 1.0,
            DeclaredValue = (double)dto.TotalPrice,
            CreatedAt = DateTime.UtcNow.ToString("O"),
            ConfirmedAt = string.Empty
        };
    }
}
