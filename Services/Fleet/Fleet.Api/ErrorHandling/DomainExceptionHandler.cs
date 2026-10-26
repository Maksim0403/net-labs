using Fleet.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Fleet.Api.ErrorHandling;

/// <summary>Централізована обробка помилок (ProblemDetails).</summary>
public sealed class DomainExceptionHandler(
    IProblemDetailsService problemDetails,
    ILogger<DomainExceptionHandler> logger,
    IHostEnvironment env) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception ex, CancellationToken ct)
    {
        var (status, title) = ex switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                => (StatusCodes.Status409Conflict, "Duplicate value"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
        };

        if (status >= 500)
            logger.LogError(ex, "Unhandled exception on {Path}", http.Request.Path);
        else
            logger.LogWarning("{ExceptionType} on {Path}: {Message}", ex.GetType().Name, http.Request.Path, ex.Message);

        var detail = status >= 500 && !env.IsDevelopment()
            ? "An unexpected error occurred."
            : status == StatusCodes.Status409Conflict && ex is DbUpdateException
                ? "The change conflicts with existing data."
                : ex.Message;

        http.Response.StatusCode = status;
        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            Exception = ex,
            ProblemDetails = { Status = status, Title = title, Detail = detail }
        });
    }
}

