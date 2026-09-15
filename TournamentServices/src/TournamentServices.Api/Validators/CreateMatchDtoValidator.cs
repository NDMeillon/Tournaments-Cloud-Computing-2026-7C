using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class CreateMatchDtoValidator : AbstractValidator<CreateMatchDto>
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

    public CreateMatchDtoValidator()
    {
        RuleFor(x => x.HomeTeamId)
            .NotEmpty().WithMessage("Home team ID is required.")
            .Matches(IdPattern).WithMessage("Home team ID must match format [A-Za-z0-9-]");

        RuleFor(x => x.VisitorTeamId)
            .NotEmpty().WithMessage("Visitor team ID is required.")
            .Matches(IdPattern).WithMessage("Visitor team ID must match format [A-Za-z0-9-]");

        RuleFor(x => x.VisitorTeamId)
            .NotEqual(x => x.HomeTeamId)
            .WithMessage("Home team and visitor team must be different.");

        When(x => !string.IsNullOrWhiteSpace(x.GroupId), () =>
        {
            RuleFor(x => x.GroupId!)
                .Matches(IdPattern).WithMessage("Group ID must match format [A-Za-z0-9-]");
        });
    }
}