using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Reviews.WebApi.HealthChecks;

public class ReviewsHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Reviews service operational"));
    }
}

