using MediatR;
using Microsoft.Extensions.Logging;

namespace Reviews.Application.Behaviors;

/// <summary>Поведінка пайплайну для структурованого логування запитів і відповідей.</summary>
public sealed class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        _logger.LogInformation("Початок виконання запиту {RequestName}: {@Request}", requestName, request);

        try
        {
            var response = await next();
            _logger.LogInformation("Успішне завершення запиту {RequestName}", requestName);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Помилка під час виконання запиту {RequestName}", requestName);
            throw;
        }
    }
}

