using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Routes;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class TeamRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TeamRoutesTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        TeamRoutes.ResetDb();
    }

    // --- GET /teams ---

    [Fact]
    public async Task GetAllTeams_WhenNoTeamsInDb_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        teams.Should().NotBeNull();
        teams.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTeams_WhenTeamsExist_Returns200WithTeamsArray()
    {
        await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Tigres"));
        await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Rayados"));

        var response = await _client.GetAsync("/teams");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var teams = await response.Content.ReadFromJsonAsync<List<TeamDto>>();
        teams.Should().NotBeNull();
        teams!.Should().HaveCount(2);
    }

    // --- GET /teams/{id} ---

    [Fact]
    public async Task GetTeamById_WhenIdExists_Returns200WithTeam()
    {
        var created = await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Tigres"));
        var team = await created.Content.ReadFromJsonAsync<TeamDto>();

        var response = await _client.GetAsync($"/teams/{team!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<TeamDto>();
        result.Should().NotBeNull();
        result!.Id.Should().Be(team.Id);
        result.Name.Should().Be("Tigres");
    }

    [Fact]
    public async Task GetTeamById_WhenIdNotFound_Returns404NotFound()
    {
        var response = await _client.GetAsync("/teams/non-existent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTeamById_WhenIdIsInvalidFormat_Returns400BadRequest()
    {
        var response = await _client.GetAsync("/teams/team_id_with_symbols!");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- POST /teams ---

    [Fact]
    public async Task CreateTeam_WhenPayloadIsValid_Returns201CreatedAndLocationHeader()
    {
        var request = new CreateTeamDto("Toluca");

        var response = await _client.PostAsJsonAsync("/teams", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var body = await response.Content.ReadFromJsonAsync<TeamDto>();
        body.Should().NotBeNull();
        body!.Name.Should().Be("Toluca");
        response.Headers.Location!.ToString().Should().Be($"/teams/{body.Id}");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public async Task CreateTeam_WhenNameIsMissingOrEmpty_Returns400BadRequest(string invalidName)
    {
        var request = new CreateTeamDto(invalidName);

        var response = await _client.PostAsJsonAsync("/teams", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTeam_WhenNameIsDuplicate_Returns400BadRequest()
    {
        await _client.PostAsJsonAsync("/teams", new CreateTeamDto("America"));

        var response = await _client.PostAsJsonAsync("/teams", new CreateTeamDto("America"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- PUT /teams/{id} ---

    [Fact]
    public async Task UpdateTeam_WhenValid_Returns200WithUpdatedTeam()
    {
        var created = await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Guadalajara"));
        var team = await created.Content.ReadFromJsonAsync<TeamDto>();

        var updateRequest = new UpdateTeamDto("Chivas");
        var response = await _client.PutAsJsonAsync($"/teams/{team!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TeamDto>();
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(team.Id);
        updated.Name.Should().Be("Chivas");
    }

    [Fact]
    public async Task UpdateTeam_WhenIdNotFound_Returns404NotFound()
    {
        var updateRequest = new UpdateTeamDto("Chivas");

        var response = await _client.PutAsJsonAsync("/teams/non-existent-team", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- DELETE /teams/{id} ---

    [Fact]
    public async Task DeleteTeam_WhenIdExists_Returns204NoContent()
    {
        var created = await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Cruz Azul"));
        var team = await created.Content.ReadFromJsonAsync<TeamDto>();

        var response = await _client.DeleteAsync($"/teams/{team!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirmar eliminación
        var getResponse = await _client.GetAsync($"/teams/{team.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTeam_WhenIdNotFound_Returns404NotFound()
    {
        var response = await _client.DeleteAsync("/teams/non-existent-team");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}