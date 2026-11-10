using Fleet.Bll.Dto;
using Fleet.Bll.Services;
using Fleet.Domain.Exceptions;
using Fleet.Grpc;
using Grpc.Core;

namespace Fleet.Api.Grpc;

public class FleetGrpcService(ICourierService couriers) : FleetService.FleetServiceBase
{
    public override async Task<CourierReply> GetCourier(GetCourierRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Id must be positive"));

        var ct = context?.CancellationToken ?? CancellationToken.None;
        try
        {
            var dto = await couriers.GetByIdAsync(request.Id, ct); // з кешем, Частина 2
            return ToReply(dto);
        }
        catch (EntityNotFoundException ex)
        {
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
    }

    public override async Task<CouriersReply> GetCouriersByIds(GetCouriersByIdsRequest request, ServerCallContext context)
    {
        var reply = new CouriersReply();
        var ct = context?.CancellationToken ?? CancellationToken.None;
        foreach (var id in request.Ids)
        {
            if (id <= 0) continue;
            try
            {
                var dto = await couriers.GetByIdAsync(id, ct);
                reply.Couriers.Add(ToReply(dto));
            }
            catch (EntityNotFoundException)
            {
                // пропуск неіснуючих кур'єрів
            }
        }
        return reply;
    }

    private static CourierReply ToReply(CourierDetailsDto c) => new()
    {
        Id = c.Id,
        WarehouseId = c.WarehouseId,
        WarehouseCode = c.WarehouseCode ?? string.Empty,
        FullName = c.FullName,
        Phone = c.Phone,
        Status = "Active",
        WarehouseName = c.WarehouseCode ?? string.Empty
    };
}
