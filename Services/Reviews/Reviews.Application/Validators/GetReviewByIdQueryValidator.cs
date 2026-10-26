using FluentValidation;
using MongoDB.Bson;
using Reviews.Application.Queries;

namespace Reviews.Application.Validators;

/// <summary>Валідатор запиту отримання відгуку за ідентифікатором.</summary>
public sealed class GetReviewByIdQueryValidator : AbstractValidator<GetReviewByIdQuery>
{
    public GetReviewByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Невірний формат ObjectId для ідентифікатора відгуку.");
    }
}

