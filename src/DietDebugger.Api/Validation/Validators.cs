using DietDebugger.Api.Endpoints;
using DietDebugger.Application.Auth.Commands;
using FluentValidation;

namespace DietDebugger.Api.Validation;

public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Invalid email format.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.")
            .Matches(@"\d").WithMessage("Password must contain at least one number.");
    }
}

public class ProfileRequestValidator : AbstractValidator<ProfileRequest>
{
    public ProfileRequestValidator()
    {
        When(x => x.WeightKg.HasValue, () =>
            RuleFor(x => x.WeightKg!.Value)
                .InclusiveBetween(30m, 300m).WithMessage("Weight must be between 30 and 300 kg."));

        When(x => x.HeightCm.HasValue, () =>
            RuleFor(x => x.HeightCm!.Value)
                .InclusiveBetween(100, 250).WithMessage("Height must be between 100 and 250 cm."));

        When(x => x.Age.HasValue, () =>
            RuleFor(x => x.Age!.Value)
                .InclusiveBetween(10, 120).WithMessage("Age must be between 10 and 120."));
    }
}

public class GoalRequestValidator : AbstractValidator<GoalRequest>
{
    public GoalRequestValidator()
    {
        RuleFor(x => x.DailyCalorieTarget)
            .InclusiveBetween(800, 5000).WithMessage("Calorie target must be between 800 and 5000.");

        When(x => x.ProteinTargetG.HasValue, () =>
            RuleFor(x => x.ProteinTargetG!.Value)
                .InclusiveBetween(0, 500).WithMessage("Protein target must be between 0 and 500g."));

        When(x => x.FatTargetG.HasValue, () =>
            RuleFor(x => x.FatTargetG!.Value)
                .InclusiveBetween(0, 500).WithMessage("Fat target must be between 0 and 500g."));

        When(x => x.CarbsTargetG.HasValue, () =>
            RuleFor(x => x.CarbsTargetG!.Value)
                .InclusiveBetween(0, 500).WithMessage("Carbs target must be between 0 and 500g."));
    }
}

public class HabitRequestValidator : AbstractValidator<HabitRequest>
{
    public HabitRequestValidator()
    {
        RuleFor(x => x.HabitDescription)
            .NotEmpty().WithMessage("Habit description is required.")
            .MaximumLength(200).WithMessage("Habit description must not exceed 200 characters.");
    }
}
