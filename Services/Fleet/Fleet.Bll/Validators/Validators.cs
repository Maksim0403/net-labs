using Fleet.Bll.Dto;
using FluentValidation;

namespace Fleet.Bll.Validators;

// Складніші правила, яких не виразити DataAnnotations. Прості (Required, Range, довжина) лишаються на DTO.
public class CreateCourierDtoValidator : AbstractValidator<CreateCourierDto>
{
    public CreateCourierDtoValidator()
    {
        RuleFor(x => x.Phone).Matches(@"^\+?[0-9]{10,15}$")
            .WithMessage("Phone must contain 10-15 digits, optionally starting with '+'.");
        RuleFor(x => x.FullName).Must(n => !string.IsNullOrWhiteSpace(n) && n.Trim().Length >= 2)
            .WithMessage("FullName must not be blank.");
    }
}

public class CreateVehicleDtoValidator : AbstractValidator<CreateVehicleDto>
{
    public CreateVehicleDtoValidator()
    {
        RuleFor(x => x.PlateNumber).Matches(@"^[A-Za-z0-9 -]{3,20}$")
            .WithMessage("PlateNumber may contain only letters, digits, spaces and '-'.");
    }
}

public class CreateWarehouseDtoValidator : AbstractValidator<CreateWarehouseDto>
{
    public CreateWarehouseDtoValidator()
    {
        RuleFor(x => x.Code).Matches(@"^[A-Z0-9-]{2,20}$")
            .WithMessage("Code must be upper-case letters, digits or '-'.");
    }
}

public class CreateRouteDtoValidator : AbstractValidator<CreateRouteDto>
{
    public CreateRouteDtoValidator()
    {
        RuleFor(x => x.PlannedDate).Must(d => d >= DateOnly.FromDateTime(DateTime.UtcNow.Date))
            .WithMessage("PlannedDate cannot be in the past.");
    }
}
