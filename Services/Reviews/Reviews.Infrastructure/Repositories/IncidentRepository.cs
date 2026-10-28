using Reviews.Domain.Entities;
using Reviews.Domain.Repositories;
using Reviews.Infrastructure.Context;

namespace Reviews.Infrastructure.Repositories;

/// <summary>Репозиторій інцидентів доставки на базі MongoDB Driver.</summary>
public sealed class IncidentRepository : MongoRepository<Incident>, IIncidentRepository
{
    public IncidentRepository(MongoDbContext context) : base(context, "incidents")
    {
    }
}

