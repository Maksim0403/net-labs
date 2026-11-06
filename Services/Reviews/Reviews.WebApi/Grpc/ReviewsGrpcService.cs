using Grpc.Core;
using MediatR;
using Reviews.Application.Queries;
using Reviews.Grpc;

namespace Reviews.WebApi.Grpc;

public class ReviewsGrpcService(ISender sender) : ReviewsService.ReviewsServiceBase
{
    public override async Task<ReviewsReply> GetReviewsByShipment(
        GetReviewsByShipmentRequest request, ServerCallContext context)
    {
        if (request.ShipmentId <= 0)
            throw new RpcException(new Status(StatusCode.InvalidArgument, "ShipmentId must be positive"));

        var query = new GetReviewsQuery(
            CourierId: null,
            CustomerId: null,
            ShipmentId: request.ShipmentId,
            MinRating: null,
            SearchText: null,
            Page: 1,
            PageSize: 100);

        var ct = context?.CancellationToken ?? CancellationToken.None;
        var result = await sender.Send(query, ct);

        var reply = new ReviewsReply
        {
            TotalCount = result.TotalCount
        };

        foreach (var item in result.Items)
        {
            reply.Items.Add(new ReviewItemReply
            {
                Id = item.Id,
                ShipmentId = item.ShipmentId,
                CustomerId = item.CustomerId,
                Rating = item.Rating,
                Text = item.Text,
                CreatedAt = item.CreatedAt.ToString("O")
            });
        }

        return reply;
    }
}
