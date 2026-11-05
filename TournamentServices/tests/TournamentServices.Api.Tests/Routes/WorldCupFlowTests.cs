using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TournamentServices.Api.Dtos;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class WorldCupFlowTests : IClassFixture<TournamentApiFactory>, IAsyncLifetime
{
    private readonly TournamentApiFactory _factory;
    private readonly HttpClient _client;

    public WorldCupFlowTests(TournamentApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Load_GroupsThenMatchesThenKnockout_EndsWithOneChampion()
    {
        var created = await _client.PostAsJsonAsync(
            "/tournaments",
            new CreateTournamentDto("Mundial", new TournamentFormatDto(4, 2)));
        created.StatusCode.Should().Be(HttpStatusCode.Created);
        var tournament = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var groups = await _client.PostAsync($"/tournaments/{tournament!.Id}/groups/generate", null);
        groups.StatusCode.Should().Be(HttpStatusCode.OK);
        var groupList = await groups.Content.ReadFromJsonAsync<List<GroupDto>>();
        groupList.Should().HaveCount(2);

        var filled = await _client.PostAsync($"/tournaments/{tournament.Id}/groups/fill", null);
        filled.StatusCode.Should().Be(HttpStatusCode.OK);
        var full = await filled.Content.ReadFromJsonAsync<List<GroupDto>>();
        full!.Should().OnlyContain(group => group.Teams.Count == 4);

        var fixtures = await _client.PostAsync($"/tournaments/{tournament.Id}/matches/generate", null);
        fixtures.StatusCode.Should().Be(HttpStatusCode.OK);
        var matches = await fixtures.Content.ReadFromJsonAsync<List<MatchDto>>();
        matches.Should().HaveCount(12);
        matches.Should().OnlyContain(match =>
            match.Score.HomeTeamScore == 0 &&
            match.Score.VisitorTeamScore == 0 &&
            match.IsCompleted == false);

        var again = await _client.PostAsync($"/tournaments/{tournament.Id}/matches/generate", null);
        again.StatusCode.Should().Be(HttpStatusCode.OK);
        (await again.Content.ReadFromJsonAsync<List<MatchDto>>()).Should().HaveCount(12);

        var played = await _client.PostAsync($"/tournaments/{tournament.Id}/matches/simulate", null);
        played.StatusCode.Should().Be(HttpStatusCode.OK);
        var playedMatches = await played.Content.ReadFromJsonAsync<List<MatchDto>>();
        playedMatches.Should().OnlyContain(match => match.IsCompleted);

        var table = await _client.GetFromJsonAsync<List<GroupStandingDto>>($"/tournaments/{tournament.Id}/standings");
        table.Should().HaveCount(2);
        foreach (var group in table!)
        {
            group.Table.Should().HaveCount(4);
            for (var i = 1; i < group.Table.Count; i++)
                ComesBefore(group.Table[i - 1], group.Table[i]).Should().BeTrue();
        }

        var knockout = await _client.PostAsync($"/tournaments/{tournament.Id}/knockout", null);
        knockout.StatusCode.Should().Be(HttpStatusCode.OK);
        var champion = await knockout.Content.ReadFromJsonAsync<ChampionDto>();
        var knockoutMatches = champion!.Rounds.SelectMany(round => round.Matches).ToList();
        knockoutMatches.Should().HaveCount(3);
        knockoutMatches.Should().OnlyContain(match => match.Winner != null && match.Score.HomeTeamScore != match.Score.VisitorTeamScore);
        champion.Rounds.Last().Matches.Should().ContainSingle();
        champion.Name.Should().NotBeNullOrWhiteSpace();

        var qualifierIds = table.SelectMany(group => group.Table.Take(2).Select(row => row.TeamId));
        qualifierIds.Should().Contain(champion.TeamId);

        var repeat = await _client.PostAsync($"/tournaments/{tournament.Id}/knockout", null);
        var same = await repeat.Content.ReadFromJsonAsync<ChampionDto>();
        same!.TeamId.Should().Be(champion.TeamId);
        same.Rounds.SelectMany(round => round.Matches).Should().HaveCount(3);
    }

    private static bool ComesBefore(StandingRowDto previous, StandingRowDto current)
    {
        if (previous.Wins != current.Wins)
            return previous.Wins > current.Wins;
        if (previous.GoalsFor != current.GoalsFor)
            return previous.GoalsFor > current.GoalsFor;
        if (previous.GoalsAgainst != current.GoalsAgainst)
            return previous.GoalsAgainst < current.GoalsAgainst;
        return string.Compare(previous.Name, current.Name, StringComparison.OrdinalIgnoreCase) <= 0;
    }
}
