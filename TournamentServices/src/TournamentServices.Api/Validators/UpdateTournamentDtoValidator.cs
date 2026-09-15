using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class UpdateTournamentDtoValidator : AbstractValidator<UpdateTournamentDto>
{
    public UpdateTournamentDtoValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Tournament name is required.")
            .MaximumLength(100).WithMessage("Tournament name cannot exceed 100 characters.");

        RuleFor(x => x.Format)
            .NotNull().WithMessage("Tournament format is required.");

        When(x => x.Format != null, () =>
        {
            RuleFor(x => x.Format.MaxTeamsPerGroup)
                .GreaterThan(0).WithMessage("Max teams per group must be greater than 0.");

            RuleFor(x => x.Format.NumberOfGroups)
                .GreaterThan(0).WithMessage("Number of groups must be greater than 0.");

            RuleFor(x => x.Format.Type)
                .IsInEnum().WithMessage("Invalid tournament format type.");
        });
    }
}