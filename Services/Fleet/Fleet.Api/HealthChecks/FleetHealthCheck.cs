using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fleet.Api.HealthChecks;

public class FleetHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Fleet service operational"));
    }
}

