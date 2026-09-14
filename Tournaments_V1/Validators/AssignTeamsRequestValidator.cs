using FluentValidation;
using Tournaments_V1.Contracts.Requests;

namespace Tournaments_V1.Validators;

public class AssignTeamsRequestValidator : AbstractValidator<AssignTeamsRequest>
{
    public AssignTeamsRequestValidator()
    {
        RuleFor(x => x.Teams).NotEmpty();
        RuleForEach(x => x.Teams).ChildRules(team =>
        {
            team.RuleFor(t => t.Id)
                .NotEmpty()
                .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("Team ID must match format [A-Za-z0-9-]");
            team.RuleFor(t => t.Name).NotEmpty();
        });
    }
}