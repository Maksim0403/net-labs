using Grpc.Core;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace Platform.Aggregator.HealthChecks;

// gRPC Health Checking Protocol для перевірки залежностей в Aggregator (Частина 3.4, Критерій 10)
public class CatalogHealthCheck(Health.HealthClient client, ILogger<CatalogHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var reply = await client.CheckAsync(new HealthCheckRequest(),
                deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct);
            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("catalog-api (fleet-api) is serving")
                : HealthCheckResult.Unhealthy($"catalog-api (fleet-api) status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "catalog-api (fleet-api) health check failed");
            return HealthCheckResult.Unhealthy("catalog-api (fleet-api) is unavailable", ex);
        }
    }
}

public class FleetGrpcHealthCheck(Health.HealthClient client, ILogger<FleetGrpcHealthCheck> logger) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            var reply = await client.CheckAsync(new HealthCheckRequest(),
                deadline: DateTime.UtcNow.AddSeconds(2), cancellationToken: ct);
            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy("fleet-api is serving")
                : HealthCheckResult.Unhealthy($"fleet-api status: {reply.Status}");
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "fleet-api health check failed");
            return HealthCheckResult.Unhealthy("fleet-api is unavailable", ex);
        }
    }
}

