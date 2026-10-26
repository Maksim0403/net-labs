using FluentValidation.Results;

namespace Reviews.Application.Exceptions;

/// <summary>Виняток валідації рівня застосунку з групуванням помилок за полями.</summary>
public sealed class AppValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public AppValidationException() : base("Виникла одна або декілька помилок валідації.")
    {
        Errors = new Dictionary<string, string[]>();
    }

    public AppValidationException(IEnumerable<ValidationFailure> failures) : this()
    {
        Errors = failures
            .GroupBy(e => e.PropertyName, e => e.ErrorMessage)
            .ToDictionary(failureGroup => failureGroup.Key, failureGroup => failureGroup.ToArray());
    }
}

