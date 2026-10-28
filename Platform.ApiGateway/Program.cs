using System.Diagnostics;
using Platform.ServiceDefaults;
using Platform.ServiceDefaults.Correlation;
using Yarp.ReverseProxy.Model;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

var app = builder.Build();

app.MapDefaultEndpoints();

// Наскрізний CorrelationId middleware перед проксіюванням
app.UseCorrelationId();

// Логування маршрутизації в Gateway
app.Use(async (context, next) =>
{
    var sw = Stopwatch.StartNew();
    var method = context.Request.Method;
    var path = context.Request.Path.Value ?? string.Empty;

    try
    {
        await next();
    }
    finally
    {
        sw.Stop();
        var proxyFeature = context.GetReverseProxyFeature();
        var routeId = proxyFeature?.Route?.Config?.RouteId ?? context.GetEndpoint()?.DisplayName ?? "direct";
        var clusterId = proxyFeature?.Cluster?.Config?.ClusterId ?? "none";
        var statusCode = context.Response.StatusCode;

        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        logger.LogInformation(
            "Gateway routed request: Method: {Method}, Path: {Path}, RouteId: {RouteId}, ClusterId: {ClusterId}, StatusCode: {StatusCode}, DurationMs: {DurationMs}ms",
            method, path, routeId, clusterId, statusCode, sw.ElapsedMilliseconds);
    }
});

app.MapReverseProxy();

app.Run();

public partial class Program { }

