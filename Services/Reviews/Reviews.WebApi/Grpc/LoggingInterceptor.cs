using System.Diagnostics;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace Reviews.WebApi.Grpc;

public class LoggingInterceptor(ILogger<LoggingInterceptor> logger) : Interceptor
{
    public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
        TRequest request, ServerCallContext context, UnaryServerMethod<TRequest, TResponse> continuation)
    {
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await continuation(request, context);
            logger.LogInformation("gRPC {Method} from {Peer}: OK in {Elapsed} ms",
                context.Method, context.Peer, sw.ElapsedMilliseconds);
            return response;
        }
        catch (RpcException ex)
        {
            logger.LogWarning(ex, "gRPC {Method}: {Status}", context.Method, ex.StatusCode);
            throw;
        }
    }
}

