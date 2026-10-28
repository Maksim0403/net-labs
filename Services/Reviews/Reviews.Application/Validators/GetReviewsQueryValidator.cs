using FluentValidation;
using Reviews.Application.Queries;

namespace Reviews.Application.Validators;

/// <summary>Валідатор запиту пагінації та фільтрації списку відгуків.</summary>
public sealed class GetReviewsQueryValidator : AbstractValidator<GetReviewsQuery>
{
    public GetReviewsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Номер сторінки повинен бути не менше 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("Розмір сторінки повинен бути від 1 до 100.");

        When(x => x.MinRating.HasValue, () =>
        {
            RuleFor(x => x.MinRating!.Value)
                .InclusiveBetween(1, 5)
                .WithMessage("Мінімальний рейтинг має бути від 1 до 5.");
        });

        When(x => x.CourierId.HasValue, () =>
        {
            RuleFor(x => x.CourierId!.Value)
                .GreaterThan(0)
                .WithMessage("Ідентифікатор кур'єра має бути додатним числом.");
        });

        When(x => x.CustomerId.HasValue, () =>
        {
            RuleFor(x => x.CustomerId!.Value)
                .GreaterThan(0)
                .WithMessage("Ідентифікатор клієнта має бути додатним числом.");
        });

        When(x => x.ShipmentId.HasValue, () =>
        {
            RuleFor(x => x.ShipmentId!.Value)
                .GreaterThan(0)
                .WithMessage("Ідентифікатор відправлення має бути додатним числом.");
        });
    }
}

