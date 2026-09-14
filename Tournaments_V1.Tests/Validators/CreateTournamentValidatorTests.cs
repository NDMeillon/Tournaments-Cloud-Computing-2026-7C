using FluentValidation.TestHelper;
using Tournaments_V1.Contracts.Requests;
using Tournaments_V1.Validators;

namespace Tournaments_V1.Tests.Validators;

public class CreateTournamentValidatorTests
{
    private readonly CreateTournamentRequestValidator _validator = new();

    [Theory]
    [InlineData("torneo_2026!")]
    [InlineData("espacio en id")]
    [InlineData("torneo@")]
    public void Should_Have_Error_When_Id_Does_Not_Match_Regex(string invalidId)
    {
        var request = new CreateTournamentRequest(invalidId, "Copa Premier", DateTime.UtcNow.AddDays(5));

        var result = _validator.TestValidate(request);

        result.ShouldHaveValidationErrorFor(x => x.Id)
              .WithErrorMessage("ID must match format [A-Za-z0-9-]");
    }

    [Fact]
    public void Should_Pass_When_Request_Is_Valid()
    {
        var request = new CreateTournamentRequest("copa-2026", "Copa Premier", DateTime.UtcNow.AddDays(5));

        var result = _validator.TestValidate(request);

        result.ShouldNotHaveAnyValidationErrors();
    }
}