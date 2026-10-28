using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Behaviors;

/// <summary>Поведінка пайплайну для відстеження часу виконання та попередження про повільні запити.</summary>
public sealed class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly Stopwatch _timer = new();
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;
    private const int SlowRequestThresholdMs = 500;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _timer.Restart();

        var response = await next();

        _timer.Stop();
        var elapsedMilliseconds = _timer.ElapsedMilliseconds;

        if (elapsedMilliseconds > SlowRequestThresholdMs)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Повільний обробник запиту: {RequestName} тривав {ElapsedMilliseconds}ms (поріг {Threshold}ms)",
                requestName,
                elapsedMilliseconds,
                SlowRequestThresholdMs);
        }

        return response;
    }
}

