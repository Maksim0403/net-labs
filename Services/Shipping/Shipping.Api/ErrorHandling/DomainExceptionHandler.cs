using Microsoft.AspNetCore.Diagnostics;
using Shipping.Domain.Exceptions;

namespace Shipping.Api.ErrorHandling;

/// <summary>
/// Централізована обробка помилок (Крок D): перетворює доменні винятки
/// на HTTP-статуси й ProblemDetails. Жодних try/catch у контролерах.
/// </summary>
public sealed class DomainExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<DomainExceptionHandler> _logger;

    public DomainExceptionHandler(IProblemDetailsService problemDetails, ILogger<DomainExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            BusinessConflictException => StatusCodes.Status409Conflict,
            ValidationException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Необроблений виняток. TraceId: {TraceId}", httpContext.TraceIdentifier);
        else
            _logger.LogWarning("{Exception}: {Message}. TraceId: {TraceId}",
                exception.GetType().Name, exception.Message, httpContext.TraceIdentifier);

        httpContext.Response.StatusCode = statusCode;

        var isServerError = statusCode == StatusCodes.Status500InternalServerError;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails =
            {
                Title = isServerError ? "Internal Server Error" : exception.GetType().Name,
                Detail = isServerError
                    ? "An unexpected error occurred."
                    : exception.Message,
                Status = statusCode
            }
        });
    }
}
