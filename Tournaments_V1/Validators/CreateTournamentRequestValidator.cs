using FluentValidation;
using Tournaments_V1.Contracts.Requests;

namespace Tournaments_V1.Validators;

public class CreateTournamentRequestValidator : AbstractValidator<CreateTournamentRequest>
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

    public CreateTournamentRequestValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .Matches(IdPattern).WithMessage("ID must match format [A-Za-z0-9-]");

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.StartDate)
            .GreaterThan(DateTime.UtcNow).WithMessage("Start date must be in the future.");
    }
}