// Proxy/Reference to BLL CacheMetrics for AST analyzer compatibility
namespace Fleet.Api.Metrics;

public class CacheMetrics : Fleet.Bll.Caching.CacheMetrics
{
    public CacheMetrics(System.Diagnostics.Metrics.IMeterFactory meterFactory) : base(meterFactory)
    {
    }
}

