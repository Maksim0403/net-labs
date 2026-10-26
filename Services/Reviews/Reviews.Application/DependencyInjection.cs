using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Reviews.Application.Behaviors;

namespace Reviews.Application;

public static class DependencyInjection
{
    /// <summary>Реєстрація сервісів прикладного рівня: MediatR, Pipeline Behaviors, FluentValidation.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);

            // Порядок behaviors: Logging -> Performance -> Validation -> Handler
            cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
            cfg.AddOpenBehavior(typeof(PerformanceBehavior<,>));
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}

