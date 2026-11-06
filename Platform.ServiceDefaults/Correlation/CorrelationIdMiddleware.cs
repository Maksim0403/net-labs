using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;
using System.Diagnostics;

namespace Platform.ServiceDefaults.Correlation;

public class CorrelationIdMiddleware
{
    public const string HeaderKey = "X-Correlation-Id";
    public const string LogPropertyName = "CorrelationId";

    private readonly RequestDelegate _next;

    public CorrelationIdMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        string correlationId;
        if (context.Request.Headers.TryGetValue(HeaderKey, out var headerValues) &&
            !string.IsNullOrWhiteSpace(headerValues.FirstOrDefault()))
        {
            correlationId = headerValues.First()!;
        }
        else
        {
            correlationId = Guid.NewGuid().ToString("D");
            context.Request.Headers[HeaderKey] = correlationId;
        }

        context.Items[HeaderKey] = correlationId;
        context.Items[LogPropertyName] = correlationId;

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderKey] = correlationId;
            return Task.CompletedTask;
        });

        Activity.Current?.SetTag("correlation_id", correlationId);

        using (LogContext.PushProperty(LogPropertyName, correlationId))
        {
            await _next(context);
        }
    }
}

public static class CorrelationIdMiddlewareExtensions
{
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app)
    {
        return app.UseMiddleware<CorrelationIdMiddleware>();
    }
}
