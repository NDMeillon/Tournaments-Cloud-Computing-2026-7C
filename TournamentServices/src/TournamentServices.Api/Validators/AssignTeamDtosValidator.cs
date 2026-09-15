using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class AssignTeamsDtoValidator : AbstractValidator<AssignTeamsDto>
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

    public AssignTeamsDtoValidator()
    {
        RuleFor(x => x.TeamIds)
            .NotNull().WithMessage("Team IDs list is required.")
            .NotEmpty().WithMessage("At least one team ID must be provided.");

        RuleForEach(x => x.TeamIds)
            .NotEmpty().WithMessage("Team ID cannot be empty.")
            .Matches(IdPattern).WithMessage("Team ID must match format [A-Za-z0-9-]");

        RuleFor(x => x.TeamIds)
            .Must(ids => ids == null || ids.Distinct().Count() == ids.Count)
            .WithMessage("Duplicate team IDs are not allowed in the same assignment.");
    }
}