using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TournamentServices.Api.Dtos;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class MatchRoutesTests : IClassFixture<TournamentApiFactory>, IAsyncLifetime
{
    private readonly TournamentApiFactory _factory;
    private readonly HttpClient _client;

    public MatchRoutesTests(TournamentApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task CreateMatch_WhenTeamsShareGroup_Returns201()
    {
        var (tournament, group, home, away) = await SeedGroupAsync();

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, away.Id));

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var match = await response.Content.ReadFromJsonAsync<MatchDto>();
        match.Should().NotBeNull();
        match!.HomeTeamId.Should().Be(home.Id);
        match.VisitorTeamId.Should().Be(away.Id);
        match.HomeTeam!.Name.Should().Be("Tigres");
        match.IsCompleted.Should().BeFalse();
        match.Winner.Should().BeNull();
        response.Headers.Location!.ToString().Should().EndWith($"/tournaments/{tournament.Id}/matches/{match.Id}");
    }

    [Fact]
    public async Task CreateMatch_WhenSameTeam_Returns422()
    {
        var (tournament, group, home, _) = await SeedGroupAsync();

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, home.Id));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task CreateMatch_WhenTeamIsOutsideTheGroup_Returns422()
    {
        var (tournament, group, home, _) = await SeedGroupAsync();
        var outsider = await CreateTeamAsync("Pumas");

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, outsider.Id));

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task UpdateScore_ComputesWinner()
    {
        var (tournament, group, home, away) = await SeedGroupAsync();
        var created = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, away.Id));
        var match = await created.Content.ReadFromJsonAsync<MatchDto>();

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches/{match!.Id}/score",
            new UpdateScoreDto(3, 1));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MatchDto>();
        updated!.Score.HomeTeamScore.Should().Be(3);
        updated.Score.VisitorTeamScore.Should().Be(1);
        updated.Winner.Should().Be(MatchWinner.HOME);
        updated.IsCompleted.Should().BeTrue();
        updated.VisitorTeam!.Name.Should().Be("Rayados");
    }

    [Fact]
    public async Task UpdateScore_Tie_LeavesWinnerNull()
    {
        var (tournament, group, home, away) = await SeedGroupAsync();
        var created = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, away.Id));
        var match = await created.Content.ReadFromJsonAsync<MatchDto>();

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches/{match!.Id}/score",
            new UpdateScoreDto(2, 2));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<MatchDto>();
        updated!.Winner.Should().BeNull();
        updated.IsCompleted.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateScore_WhenNegative_Returns400()
    {
        var (tournament, group, home, away) = await SeedGroupAsync();
        var created = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, away.Id));
        var match = await created.Content.ReadFromJsonAsync<MatchDto>();

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches/{match!.Id}/score",
            new UpdateScoreDto(-1, 0));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteMatch_WhenMissing_Returns404()
    {
        var tournament = await CreateTournamentAsync();

        var response = await _client.DeleteAsync($"/tournaments/{tournament.Id}/matches/missing-match");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMatches_ReturnsCreatedMatch()
    {
        var (tournament, group, home, away) = await SeedGroupAsync();
        await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/matches",
            new CreateMatchDto(group.Id, home.Id, away.Id));

        var response = await _client.GetAsync($"/tournaments/{tournament.Id}/matches");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var matches = await response.Content.ReadFromJsonAsync<List<MatchDto>>();
        matches.Should().ContainSingle();
    }

    private async Task<(TournamentDto Tournament, GroupDto Group, TeamDto Home, TeamDto Away)> SeedGroupAsync()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var home = await CreateTeamAsync("Tigres");
        var away = await CreateTeamAsync("Rayados");
        var assign = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams",
            new AssignTeamsDto([home.Id, away.Id]));
        assign.StatusCode.Should().Be(HttpStatusCode.NoContent);
        return (tournament, group, home, away);
    }

    private async Task<TournamentDto> CreateTournamentAsync()
    {
        var response = await _client.PostAsJsonAsync(
            "/tournaments",
            new CreateTournamentDto("Copa", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TournamentDto>())!;
    }

    private async Task<GroupDto> CreateGroupAsync(string tournamentId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournamentId}/groups",
            new CreateGroupDto(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<GroupDto>())!;
    }

    private async Task<TeamDto> CreateTeamAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/teams", new CreateTeamDto(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TeamDto>())!;
    }
}
