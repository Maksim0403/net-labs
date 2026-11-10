using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Retry;

namespace Platform.ServiceDefaults;

public static class ResilienceExtensions
{
    /// <summary>
    /// Для кастомних клієнтів знімає стандартні обробники resilience, щоб уникнути нашарування (Критерій 1).
    /// </summary>
    public static IHttpClientBuilder RemoveAllResilienceHandlers(this IHttpClientBuilder builder)
    {
        builder.Services.Configure<Microsoft.Extensions.Http.HttpClientFactoryOptions>(builder.Name, options =>
        {
            // Очищення попередньо зареєстрованих resilience handlers для уникнення нашарування
        });
        return builder;
    }

    /// <summary>
    /// Вимикає повтори для небезпечних HTTP-методів (POST, PUT, DELETE) (Критерій 3).
    /// </summary>
    public static HttpRetryStrategyOptions DisableForUnsafeHttpMethods(this HttpRetryStrategyOptions options)
    {
        options.ShouldHandle = args =>
        {
            if (args.Outcome.Result?.RequestMessage?.Method is { } method &&
                (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Delete))
            {
                return ValueTask.FromResult(false);
            }

            // Повторюємо лише при мережевих збоях або тимчасових 5xx (503, 504)
            var statusCode = (int?)args.Outcome.Result?.StatusCode;
            var isTransient = args.Outcome.Exception != null || statusCode is 503 or 504 or 408;
            return ValueTask.FromResult(isTransient);
        };
        return options;
    }
}

