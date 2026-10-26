using FluentValidation;
using MongoDB.Bson;
using Reviews.Application.Commands;

namespace Reviews.Application.Validators;

/// <summary>Валідатор команди оновлення відгуку з перевіркою формату ObjectId.</summary>
public sealed class UpdateReviewCommandValidator : AbstractValidator<UpdateReviewCommand>
{
    public UpdateReviewCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Ідентифікатор відгуку є обов'язковим.")
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Невірний формат ObjectId для ідентифікатора відгуку.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Оцінка повинна бути від 1 до 5.");

        RuleFor(x => x.Text)
            .MaximumLength(2000)
            .WithMessage("Текст відгуку не може перевищувати 2000 символів.");

        RuleFor(x => x.Version)
            .GreaterThan(0)
            .WithMessage("Версія відгуку має бути більшою за 0.");

        When(x => x.Criteria is not null, () =>
        {
            RuleFor(x => x.Criteria!.Speed)
                .InclusiveBetween(1, 5)
                .WithMessage("Оцінка швидкості має бути від 1 до 5.");

            RuleFor(x => x.Criteria!.Politeness)
                .InclusiveBetween(1, 5)
                .WithMessage("Оцінка ввічливості має бути від 1 до 5.");

            RuleFor(x => x.Criteria!.Packaging)
                .InclusiveBetween(1, 5)
                .WithMessage("Оцінка пакування має бути від 1 до 5.");
        });
    }
}

