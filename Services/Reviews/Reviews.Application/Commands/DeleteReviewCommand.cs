using MediatR;
using MongoDB.Bson;
using Reviews.Application.Common;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Commands;

/// <summary>Команда видалення відгуку за ідентифікатором і очікуваною версією.</summary>
public sealed record DeleteReviewCommand(string Id, int Version) : ICommand<Unit>;

public sealed class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand, Unit>
{
    private readonly IReviewRepository _repository;

    public DeleteReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task<Unit> Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        await _repository.DeleteAsync(objectId, request.Version, cancellationToken);
        return Unit.Value;
    }
}

