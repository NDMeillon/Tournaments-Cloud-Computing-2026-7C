using System.Net;
using System.Net.Http.Json;
using TournamentServices.Api.Dtos;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class GroupRoutesTests : IClassFixture<TournamentApiFactory>, IAsyncLifetime
{
    private readonly TournamentApiFactory _factory;
    private readonly HttpClient _client;

    public GroupRoutesTests(TournamentApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => _factory.ResetAsync();
    public Task DisposeAsync() => Task.CompletedTask;

    private const string InvalidId = "tour_2026!";

    [Fact]
    public async Task GetGroups_WhenTournamentHasGroups_ReturnsOk()
    {
        var tournament = await CreateTournamentAsync();
        await CreateGroupAsync(tournament.Id, "Grupo A");
        await CreateGroupAsync(tournament.Id, "Grupo B");

        var response = await _client.GetAsync($"/tournaments/{tournament.Id}/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var groups = await response.Content.ReadFromJsonAsync<List<GroupDto>>();
        Assert.NotNull(groups);
        Assert.Equal(2, groups!.Count);
        Assert.All(groups, g => Assert.Equal(tournament.Id, g.TournamentId));
        Assert.Contains(groups, g => g.Name == "Grupo A");
        Assert.Contains(groups, g => g.Name == "Grupo B");
    }

    [Fact]
    public async Task GetGroups_WhenTournamentDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/tournaments/tour-missing/groups");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData("tour_2026")]
    [InlineData("tour 2026")]
    [InlineData("tour@2026")]
    public async Task GetGroups_WithInvalidTournamentId_ReturnsBadRequest(string tournamentId)
    {
        var response = await _client.GetAsync($"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("Invalid Tournament ID format.", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetGroupById_WithValidIds_ReturnsOkWithTeams()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var team = await CreateTeamAsync("Tigres");
        var assign = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams",
            new AssignTeamsDto([team.Id]));
        Assert.Equal(HttpStatusCode.NoContent, assign.StatusCode);

        var response = await _client.GetAsync($"/tournaments/{tournament.Id}/groups/{group.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(body);
        Assert.Equal(group.Id, body!.Id);
        Assert.Contains(body.Teams, t => t.Id == team.Id);
    }

    [Fact]
    public async Task GetGroupById_WhenMissing_ReturnsNotFound()
    {
        var tournament = await CreateTournamentAsync();

        var response = await _client.GetAsync($"/tournaments/{tournament.Id}/groups/missing-group");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidId, "group-a")]
    [InlineData("tour-2026", "grupo a")]
    [InlineData(InvalidId, "grupo a")]
    public async Task GetGroupById_WithInvalidIds_ReturnsBadRequest(string tournamentId, string groupId)
    {
        var response = await _client.GetAsync(
            $"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups/{Uri.EscapeDataString(groupId)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithValidPayload_ReturnsCreatedWithLocationHeader()
    {
        var tournament = await CreateTournamentAsync();
        var payload = new CreateGroupDto("Grupo C");

        var response = await _client.PostAsJsonAsync($"/tournaments/{tournament.Id}/groups", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(created);
        Assert.Equal("Grupo C", created!.Name);
        Assert.Equal(tournament.Id, created.TournamentId);
        Assert.StartsWith("grp-", created.Id);
        Assert.Equal(12, created.Id.Length);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/tournaments/{tournament.Id}/groups/{created.Id}",
            response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CreateGroup_WhenTournamentDoesNotExist_ReturnsNotFound()
    {
        var response = await _client.PostAsJsonAsync(
            "/tournaments/tour-missing/groups",
            new CreateGroupDto("Grupo C"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithDuplicateName_ReturnsUnprocessableEntity()
    {
        var tournament = await CreateTournamentAsync();
        await CreateGroupAsync(tournament.Id, "Grupo A");

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups",
            new CreateGroupDto("Grupo A"));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithInvalidTournamentId_ReturnsBadRequest()
    {
        var payload = new CreateGroupDto("Grupo C");

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{Uri.EscapeDataString(InvalidId)}/groups", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateGroup_WithInvalidBody_IsRejectedByValidationFilter()
    {
        var tournament = await CreateTournamentAsync();
        var payload = new CreateGroupDto(string.Empty);

        var response = await _client.PostAsJsonAsync($"/tournaments/{tournament.Id}/groups", payload);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 del ValidationFilter, se obtuvo {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task UpdateGroup_WithValidPayload_ReturnsOkWithUpdatedName()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var payload = new UpdateGroupDto("Grupo A - Renombrado");

        var response = await _client.PutAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(updated);
        Assert.Equal(group.Id, updated!.Id);
        Assert.Equal("Grupo A - Renombrado", updated.Name);
        Assert.Equal(tournament.Id, updated.TournamentId);
    }

    [Fact]
    public async Task UpdateGroup_WhenMissing_ReturnsNotFound()
    {
        var tournament = await CreateTournamentAsync();

        var response = await _client.PutAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/missing-group",
            new UpdateGroupDto("Grupo válido"));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidId, "group-a")]
    [InlineData("tour-2026", "group a")]
    public async Task UpdateGroup_WithInvalidIds_ReturnsBadRequest(string tournamentId, string groupId)
    {
        var payload = new UpdateGroupDto("Grupo válido");

        var response = await _client.PutAsJsonAsync(
            $"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups/{Uri.EscapeDataString(groupId)}", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DeleteGroup_WithValidIds_ReturnsNoContent()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");

        var response = await _client.DeleteAsync($"/tournaments/{tournament.Id}/groups/{group.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());

        var get = await _client.GetAsync($"/tournaments/{tournament.Id}/groups/{group.Id}");
        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
    }

    [Theory]
    [InlineData(InvalidId, "group-a")]
    [InlineData("tour-2026", "group$a")]
    public async Task DeleteGroup_WithInvalidIds_ReturnsBadRequest(string tournamentId, string groupId)
    {
        var response = await _client.DeleteAsync(
            $"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups/{Uri.EscapeDataString(groupId)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_WithValidPayload_ReturnsNoContent()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var home = await CreateTeamAsync("Tigres");
        var away = await CreateTeamAsync("Rayados");

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams",
            new AssignTeamsDto([home.Id, away.Id]));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_WhenTeamDoesNotExist_ReturnsUnprocessableEntity()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams",
            new AssignTeamsDto(["team-missing"]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_WhenExceedingMax_ReturnsUnprocessableEntity()
    {
        var tournament = await CreateTournamentAsync(maxTeams: 1);
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var home = await CreateTeamAsync("Tigres");
        var away = await CreateTeamAsync("Rayados");

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams",
            new AssignTeamsDto([home.Id, away.Id]));

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidId, "group-a")]
    [InlineData("tour-2026", "group a")]
    public async Task AssignTeams_WithInvalidIds_ReturnsBadRequest(string tournamentId, string groupId)
    {
        var payload = new AssignTeamsDto(["team-1"]);

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups/{Uri.EscapeDataString(groupId)}/teams", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignTeams_WithInvalidBody_IsRejectedByValidationFilter()
    {
        var tournament = await CreateTournamentAsync();
        var group = await CreateGroupAsync(tournament.Id, "Grupo A");
        var payload = new AssignTeamsDto([]);

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}/groups/{group.Id}/teams", payload);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 del ValidationFilter, se obtuvo {(int)response.StatusCode}.");
    }

    private async Task<TournamentDto> CreateTournamentAsync(int maxTeams = 4)
    {
        var response = await _client.PostAsJsonAsync(
            "/tournaments",
            new CreateTournamentDto("Copa", new TournamentFormatDto(maxTeams, 4, TournamentType.ROUND_ROBIN)));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TournamentDto>())!;
    }

    private async Task<GroupDto> CreateGroupAsync(string tournamentId, string name)
    {
        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{tournamentId}/groups",
            new CreateGroupDto(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<GroupDto>())!;
    }

    private async Task<TeamDto> CreateTeamAsync(string name)
    {
        var response = await _client.PostAsJsonAsync("/teams", new CreateTeamDto(name));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<TeamDto>())!;
    }
}
