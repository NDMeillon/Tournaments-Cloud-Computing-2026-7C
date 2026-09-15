using FluentValidation;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Validators;

public class UpdateScoreDtoValidator : AbstractValidator<UpdateScoreDto>
{
    public UpdateScoreDtoValidator()
    {
        RuleFor(x => x.HomeTeamScore)
            .GreaterThanOrEqualTo(0).WithMessage("Home team score cannot be negative.");

        RuleFor(x => x.VisitorTeamScore)
            .GreaterThanOrEqualTo(0).WithMessage("Visitor team score cannot be negative.");
    }
}