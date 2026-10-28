using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Platform.ServiceDefaults.Correlation;

public class CorrelationIdDelegatingHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CorrelationIdDelegatingHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!request.Headers.Contains(CorrelationIdMiddleware.HeaderKey))
        {
            var correlationId = GetCurrentCorrelationId();
            if (!string.IsNullOrWhiteSpace(correlationId))
            {
                request.Headers.TryAddWithoutValidation(CorrelationIdMiddleware.HeaderKey, correlationId);
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private string? GetCurrentCorrelationId()
    {
        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext != null)
        {
            if (httpContext.Items.TryGetValue(CorrelationIdMiddleware.HeaderKey, out var itemValue) &&
                itemValue is string str && !string.IsNullOrWhiteSpace(str))
            {
                return str;
            }

            if (httpContext.Request.Headers.TryGetValue(CorrelationIdMiddleware.HeaderKey, out var headerValue) &&
                !string.IsNullOrWhiteSpace(headerValue.FirstOrDefault()))
            {
                return headerValue.First();
            }
        }

        var activity = Activity.Current;
        var tag = activity?.GetTagItem("correlation_id")?.ToString();
        if (!string.IsNullOrWhiteSpace(tag))
        {
            return tag;
        }

        return Guid.NewGuid().ToString("D");
    }
}

