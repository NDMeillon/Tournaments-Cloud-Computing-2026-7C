using FluentValidation;
using Tournaments_V1.Contracts.Requests;

namespace Tournaments_V1.Validators;

public class AssignGroupsRequestValidator : AbstractValidator<AssignGroupsRequest>
{
    public AssignGroupsRequestValidator()
    {
        RuleFor(x => x.Groups).NotEmpty();
        RuleForEach(x => x.Groups).ChildRules(group =>
        {
            group.RuleFor(g => g.Id)
                .NotEmpty()
                .Matches(@"^[A-Za-z0-9\-]+$").WithMessage("Group ID must match format [A-Za-z0-9-]");
            group.RuleFor(g => g.Name).NotEmpty();
        });
    }
}