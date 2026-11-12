using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Shipping.Api.HealthChecks;

public class ShippingHealthCheck : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(HealthCheckResult.Healthy("Shipping service operational"));
    }
}

