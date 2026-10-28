using FluentValidation;
using Reviews.Application.Commands;
using Reviews.Domain.Repositories;

namespace Reviews.Application.Validators;

/// <summary>Валідатор команди створення відгуку з асинхронним правилом перевірки унікальності в БД.</summary>
public sealed class CreateReviewCommandValidator : AbstractValidator<CreateReviewCommand>
{
    private static readonly string[] AllowedTypes = { "rating", "photo_review", "text" };

    public CreateReviewCommandValidator(IReviewRepository reviewRepository)
    {
        RuleFor(x => x.ShipmentId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор відправлення має бути додатним числом.");

        RuleFor(x => x.CustomerId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор клієнта має бути додатним числом.");

        RuleFor(x => x.CourierId)
            .GreaterThan(0)
            .WithMessage("Ідентифікатор кур'єра має бути додатним числом.");

        RuleFor(x => x.CourierName)
            .NotEmpty()
            .WithMessage("Ім'я кур'єра є обов'язковим.")
            .MaximumLength(150)
            .WithMessage("Ім'я кур'єра не може перевищувати 150 символів.");

        RuleFor(x => x.Type)
            .NotEmpty()
            .WithMessage("Тип відгуку є обов'язковим.")
            .Must(t => AllowedTypes.Contains(t.ToLowerInvariant()))
            .WithMessage($"Тип відгуку повинен бути одним із: {string.Join(", ", AllowedTypes)}.");

        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5)
            .WithMessage("Оцінка повинна бути від 1 до 5.");

        RuleFor(x => x.Text)
            .MaximumLength(2000)
            .WithMessage("Текст відгуку не може перевищувати 2000 символів.");

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

        RuleFor(x => x.ShipmentId)
            .MustAsync(async (cmd, shipmentId, ct) =>
                !await reviewRepository.ExistsForShipmentAndCustomerAsync(shipmentId, cmd.CustomerId, ct))
            .WithMessage("Відгук для цього відправлення від вказаного клієнта вже існує.");
    }
}

