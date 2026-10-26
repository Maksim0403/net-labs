using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using Reviews.Application.Exceptions;
using Reviews.Domain.Exceptions;

namespace Reviews.WebApi.ErrorHandling;

/// <summary>Централізований обробник винятків через ProblemDetails із підтримкою MongoDB-винятків.</summary>
public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Обробка необробленого винятку: {Message}", exception.Message);

        var (statusCode, problemDetails) = exception switch
        {
            ReviewNotFoundException notFound => (
                StatusCodes.Status404NotFound,
                new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Ресурс не знайдено",
                    Detail = notFound.Message,
                    Instance = httpContext.Request.Path
                }),

            AppValidationException valEx => (
                StatusCodes.Status400BadRequest,
                new ValidationProblemDetails(valEx.Errors)
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Помилка валідації",
                    Detail = "Один або більше параметрів запиту не пройшли перевірку.",
                    Instance = httpContext.Request.Path
                }),

            DomainValidationException domainVal => (
                StatusCodes.Status400BadRequest,
                new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Помилка доменної валідації",
                    Detail = domainVal.Message,
                    Instance = httpContext.Request.Path
                }),

            ConcurrencyException concurrency => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Конфлікт конкурентності",
                    Detail = concurrency.Message,
                    Instance = httpContext.Request.Path
                }),

            DomainConflictException conflict => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Бізнес-конфлікт даних",
                    Detail = conflict.Message,
                    Instance = httpContext.Request.Path
                }),

            MongoWriteException mongoWrite when mongoWrite.WriteError?.Category == ServerErrorCategory.DuplicateKey => (
                StatusCodes.Status409Conflict,
                new ProblemDetails
                {
                    Status = StatusCodes.Status409Conflict,
                    Title = "Конфлікт унікальності",
                    Detail = "Запис з такими унікальними даними вже існує в базі даних.",
                    Instance = httpContext.Request.Path
                }),

            MongoConnectionException => (
                StatusCodes.Status503ServiceUnavailable,
                new ProblemDetails
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Title = "Сервіс тимчасово недоступний",
                    Detail = "Не вдалося встановити з'єднання з базою даних MongoDB.",
                    Instance = httpContext.Request.Path
                }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ProblemDetails
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Title = "Внутрішня помилка сервера",
                    Detail = "Під час обробки запиту виникла непередбачувана помилка.",
                    Instance = httpContext.Request.Path
                })
        };

        httpContext.Response.StatusCode = statusCode;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}

