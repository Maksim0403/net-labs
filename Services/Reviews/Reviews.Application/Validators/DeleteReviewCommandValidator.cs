using FluentValidation;
using MongoDB.Bson;
using Reviews.Application.Commands;

namespace Reviews.Application.Validators;

/// <summary>Валідатор команди видалення відгуку.</summary>
public sealed class DeleteReviewCommandValidator : AbstractValidator<DeleteReviewCommand>
{
    public DeleteReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Невірний формат ObjectId для ідентифікатора відгуку.");

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("Версія відгуку має бути більшою за 0.");
    }
}

