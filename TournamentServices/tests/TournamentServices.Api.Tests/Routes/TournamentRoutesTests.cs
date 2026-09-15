using System.Net;
using System.Net.Http.Json;
using System.Text;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Routes;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class TournamentRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public TournamentRoutesTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
        TournamentRoutes.ResetDb();
        TeamRoutes.ResetDb();
    }

    // --- GET /tournaments ---

    [Fact]
    public async Task GetAllTournaments_WhenNoTournaments_Returns200WithEmptyArray()
    {
        var response = await _client.GetAsync("/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        tournaments.Should().NotBeNull();
        tournaments.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllTournaments_WhenTournamentsExist_Returns200WithPopulatedArray()
    {
        await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Copa Verano", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Copa Invierno", new TournamentFormatDto(6, 4, TournamentType.NFL)));

        var response = await _client.GetAsync("/tournaments");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tournaments = await response.Content.ReadFromJsonAsync<List<TournamentDto>>();
        tournaments.Should().NotBeNull();
        tournaments!.Should().HaveCount(2);
    }

    // --- GET /tournaments/{id} ---

    /*[Fact(Skip = "Requires DB")]
    public async Task GetTournamentById_WhenIdExists_Returns200WithGroupsAndMatchesPopulated()
    {
        // Simulamos un torneo con grupos y partidos ya asociados en la DB
        var seededTournament = new TournamentDto(
            Id: "trn-seeded-1",
            Name: "Champions Cup",
            Format: new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN),
            Groups: [
                new GroupDto("grp-1", "Grupo A", "trn-seeded-1", [new TeamDto("team-1", "Tigres")])
            ],
            Matches: [
                new MatchDto("mtc-1", "trn-seeded-1", "grp-1", "team-1", "team-2", null, null, new ScoreDto(0, 0), null, false)
            ]
        );
        TournamentRoutes.TournamentsDb.Add(seededTournament);

        var response = await _client.GetAsync("/tournaments/trn-seeded-1");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var tournament = await response.Content.ReadFromJsonAsync<TournamentDto>();
        tournament.Should().NotBeNull();
        tournament!.Id.Should().Be("trn-seeded-1");
        tournament.Groups.Should().HaveCount(1);
        tournament.Matches.Should().HaveCount(1);
    }*/

    [Fact]
    public async Task GetTournamentById_WhenIdNotFound_Returns404NotFound()
    {
        var response = await _client.GetAsync("/tournaments/non-existent-trn");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- POST /tournaments ---

    [Theory]
    [InlineData(TournamentType.ROUND_ROBIN)]
    [InlineData(TournamentType.NFL)]
    public async Task CreateTournament_WhenValid_Returns201CreatedAndLocation(TournamentType type)
    {
        var request = new CreateTournamentDto("Copa Continental", new TournamentFormatDto(4, 2, type));

        var response = await _client.PostAsJsonAsync("/tournaments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        response.Headers.Location.Should().NotBeNull();

        var tournament = await response.Content.ReadFromJsonAsync<TournamentDto>();
        tournament.Should().NotBeNull();
        tournament!.Format.Type.Should().Be(type);
        response.Headers.Location!.ToString().Should().Be($"/tournaments/{tournament.Id}");
    }

    [Theory]
    [InlineData("", 4, 2)] // Missing/Empty name
    [InlineData("Torneo Invalido", 0, 2)] // Invalid format: maxTeamsPerGroup <= 0
    [InlineData("Torneo Invalido", 4, 0)] // Invalid format: numberOfGroups <= 0
    public async Task CreateTournament_WhenInvalidFormatOrMissingName_Returns400BadRequest(string name, int maxTeams, int groups)
    {
        var request = new CreateTournamentDto(name, new TournamentFormatDto(maxTeams, groups, TournamentType.ROUND_ROBIN));

        var response = await _client.PostAsJsonAsync("/tournaments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateTournament_WhenInvalidTypeEnum_Returns400BadRequest()
    {
        var invalidJsonPayload = """
        {
            "name": "Torneo Raro",
            "format": {
                "maxTeamsPerGroup": 4,
                "numberOfGroups": 2,
                "type": "INVALID_FORMAT_TYPE"
            }
        }
        """;

        var content = new StringContent(invalidJsonPayload, Encoding.UTF8, "application/json");
        var response = await _client.PostAsync("/tournaments", content);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // --- PUT /tournaments/{id} ---

    [Fact]
    public async Task UpdateTournament_WhenValid_Returns200WithFullUpdate()
    {
        var created = await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Nombre Viejo", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        var original = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var updateRequest = new UpdateTournamentDto("Nombre Nuevo", new TournamentFormatDto(8, 4, TournamentType.NFL));
        var response = await _client.PutAsJsonAsync($"/tournaments/{original!.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<TournamentDto>();
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(original.Id);
        updated.Name.Should().Be("Nombre Nuevo");
        updated.Format.MaxTeamsPerGroup.Should().Be(8);
        updated.Format.NumberOfGroups.Should().Be(4);
        updated.Format.Type.Should().Be(TournamentType.NFL);
    }

    [Fact]
    public async Task UpdateTournament_WhenIdNotFound_Returns404NotFound()
    {
        var updateRequest = new UpdateTournamentDto("Torneo Fantasma", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN));

        var response = await _client.PutAsJsonAsync("/tournaments/non-existent-id", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- PATCH /tournaments/{id} ---

    [Fact]
    public async Task PatchTournament_WhenUpdatingNameOnly_Returns200()
    {
        var created = await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Nombre Base", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        var original = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var patchRequest = new PatchTournamentDto(Name: "Nombre Parcialmente Editado");
        var response = await _client.PatchAsJsonAsync($"/tournaments/{original!.Id}", patchRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Name.Should().Be("Nombre Parcialmente Editado");
        patched.Format.Type.Should().Be(TournamentType.ROUND_ROBIN);
    }

    [Fact]
    public async Task PatchTournament_WhenUpdatingFormatOnly_Returns200()
    {
        var created = await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Nombre Original", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        var original = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var patchRequest = new PatchTournamentDto(Format: new PatchTournamentFormatDto(Type: TournamentType.NFL));
        var response = await _client.PatchAsJsonAsync($"/tournaments/{original!.Id}", patchRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Name.Should().Be("Nombre Original");
        patched.Format.Type.Should().Be(TournamentType.NFL);
        patched.Format.MaxTeamsPerGroup.Should().Be(4);
    }

    [Fact]
    public async Task PatchTournament_WhenUpdatingBothNameAndFormat_Returns200()
    {
        var created = await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "Original", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        var original = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var patchRequest = new PatchTournamentDto(
            Name: "Ambos Actualizados",
            Format: new PatchTournamentFormatDto(MaxTeamsPerGroup: 12)
        );
        var response = await _client.PatchAsJsonAsync($"/tournaments/{original!.Id}", patchRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Name.Should().Be("Ambos Actualizados");
        patched.Format.MaxTeamsPerGroup.Should().Be(12);
    }

    [Fact]
    public async Task PatchTournament_WhenIdNotFound_Returns404NotFound()
    {
        var patchRequest = new PatchTournamentDto(Name: "No Existente");

        var response = await _client.PatchAsJsonAsync("/tournaments/non-existent-id", patchRequest);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // --- DELETE /tournaments/{id} ---

    [Fact]
    public async Task DeleteTournament_WhenIdExists_Returns204NoContent()
    {
        var created = await _client.PostAsJsonAsync("/tournaments", new CreateTournamentDto(
            "A Eliminar", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)));
        var tournament = await created.Content.ReadFromJsonAsync<TournamentDto>();

        var response = await _client.DeleteAsync($"/tournaments/{tournament!.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Confirmar eliminación
        var getResponse = await _client.GetAsync($"/tournaments/{tournament.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteTournament_WhenIdNotFound_Returns404NotFound()
    {
        var response = await _client.DeleteAsync("/tournaments/non-existent-id");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
     private const int NumberOfGroups = 8;
    private const int TeamsPerGroup = 4;
    private const int TotalTeams = NumberOfGroups * TeamsPerGroup; // 32

    private static readonly string[] GroupNames =
        ["Grupo A", "Grupo B", "Grupo C", "Grupo D", "Grupo E", "Grupo F", "Grupo G", "Grupo H"];

    private static readonly string[] MundialTeams =
    [
        // Grupo A
        "Mexico", "Polonia", "Arabia Saudita", "Argentina",
        // Grupo B
        "Canada", "Belgica", "Marruecos", "Croacia",
        // Grupo C
        "Estados Unidos", "Gales", "Inglaterra", "Iran",
        // Grupo D
        "Francia", "Australia", "Dinamarca", "Tunez",
        // Grupo E
        "Espana", "Costa Rica", "Alemania", "Japon",
        // Grupo F
        "Brasil", "Serbia", "Suiza", "Camerun",
        // Grupo G
        "Portugal", "Ghana", "Uruguay", "Corea del Sur",
        // Grupo H
        "Paises Bajos", "Senegal", "Ecuador", "Qatar"
    ];

    private static CreateTournamentDto MundialPayload(string name = "Copa Mundial 2026") =>
        new(name, new TournamentFormatDto(TeamsPerGroup, NumberOfGroups, TournamentType.ROUND_ROBIN));

    // ---------------- Helpers ----------------

    private async Task<TournamentDto> CreateMundialAsync(string name = "Copa Mundial 2026")
    {
        var response = await _client.PostAsJsonAsync("/tournaments", MundialPayload(name));
        response.StatusCode.Should().Be(HttpStatusCode.Created);

        var tournament = await response.Content.ReadFromJsonAsync<TournamentDto>();
        tournament.Should().NotBeNull();
        return tournament!;
    }

    private async Task<List<TeamDto>> CreateAllTeamsAsync()
    {
        var teams = new List<TeamDto>(TotalTeams);

        foreach (var name in MundialTeams)
        {
            var response = await _client.PostAsJsonAsync("/teams", new CreateTeamDto(name));
            response.StatusCode.Should().Be(HttpStatusCode.Created, $"'{name}' debe crearse sin conflicto");

            var team = await response.Content.ReadFromJsonAsync<TeamDto>();
            team.Should().NotBeNull();
            teams.Add(team!);
        }

        return teams;
    }

    private async Task<List<GroupDto>> CreateAllGroupsAsync(string tournamentId)
    {
        var groups = new List<GroupDto>(NumberOfGroups);

        foreach (var groupName in GroupNames)
        {
            var response = await _client.PostAsJsonAsync(
                $"/tournaments/{tournamentId}/groups", new CreateGroupDto(groupName));
            response.StatusCode.Should().Be(HttpStatusCode.Created);

            var group = await response.Content.ReadFromJsonAsync<GroupDto>();
            group.Should().NotBeNull();
            groups.Add(group!);
        }

        return groups;
    }

    // ---------------- Armado del torneo ----------------

    [Fact]
    public async Task Mundial_SetUp_Creates32TeamsIn8GroupsOfFour()
    {
        var tournament = await CreateMundialAsync();
        var teams = await CreateAllTeamsAsync();
        var groups = await CreateAllGroupsAsync(tournament.Id);

        tournament.Format.NumberOfGroups.Should().Be(NumberOfGroups);
        tournament.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
        tournament.Format.Type.Should().Be(TournamentType.ROUND_ROBIN);

        teams.Should().HaveCount(TotalTeams);
        groups.Should().HaveCount(NumberOfGroups);
        groups.Should().OnlyContain(g => g.TournamentId == tournament.Id);

        // Reparto: 4 equipos por grupo, en orden.
        for (var i = 0; i < groups.Count; i++)
        {
            var slice = teams.Skip(i * TeamsPerGroup).Take(TeamsPerGroup).Select(t => t.Id).ToList();
            slice.Should().HaveCount(TeamsPerGroup);

            var response = await _client.PatchAsJsonAsync(
                $"/tournaments/{tournament.Id}/groups/{groups[i].Id}/teams",
                new AssignTeamsDto(slice));

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }
    }

    [Fact]
    public void  Mundial_TeamNames_AreAllUnique()
    {
        MundialTeams.Should().HaveCount(TotalTeams);
        MundialTeams.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Mundial_CreatedTeams_HaveNoDuplicateIds()
    {
        var teams = await CreateAllTeamsAsync();

        teams.Select(t => t.Id).Should().OnlyHaveUniqueItems();
        teams.Select(t => t.Name).Should().OnlyHaveUniqueItems();

        var all = await _client.GetFromJsonAsync<List<TeamDto>>("/teams");
        all.Should().NotBeNull();
        all!.Should().HaveCount(TotalTeams);
    }

    [Fact]
    public async Task Mundial_DuplicateTeamName_IsRejected()
    {
        await CreateAllTeamsAsync();

        // "Mexico" ya existe: no puede repetirse en el torneo.
        var response = await _client.PostAsJsonAsync("/teams", new CreateTeamDto("Mexico"));

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);

        var all = await _client.GetFromJsonAsync<List<TeamDto>>("/teams");
        all!.Should().HaveCount(TotalTeams, "un nombre duplicado no debe aumentar el total");
    }

    [Fact]
    public async Task Mundial_GroupAssignments_DoNotRepeatATeamAcrossGroups()
    {
        var tournament = await CreateMundialAsync();
        var teams = await CreateAllTeamsAsync();
        var groups = await CreateAllGroupsAsync(tournament.Id);

        var assigned = new List<string>();

        for (var i = 0; i < groups.Count; i++)
        {
            var slice = teams.Skip(i * TeamsPerGroup).Take(TeamsPerGroup).Select(t => t.Id).ToList();

            slice.Should().NotIntersectWith(assigned, "ningun equipo puede estar en dos grupos");
            assigned.AddRange(slice);

            var response = await _client.PatchAsJsonAsync(
                $"/tournaments/{tournament.Id}/groups/{groups[i].Id}/teams",
                new AssignTeamsDto(slice));

            response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        }

        assigned.Should().HaveCount(TotalTeams);
        assigned.Should().OnlyHaveUniqueItems();
        assigned.Should().BeEquivalentTo(teams.Select(t => t.Id));
    }

    // ---------------- PUT ----------------

    [Fact]
    public async Task Put_Mundial_ChangesNameAndFormatKeepingId()
    {
        var tournament = await CreateMundialAsync("Copa Mundial 2026");

        var updateRequest = new UpdateTournamentDto(
            "Copa Mundial 2026 - Fase Final",
            new TournamentFormatDto(TeamsPerGroup, NumberOfGroups, TournamentType.NFL));

        var response = await _client.PutAsJsonAsync($"/tournaments/{tournament.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await response.Content.ReadFromJsonAsync<TournamentDto>();
        updated.Should().NotBeNull();
        updated!.Id.Should().Be(tournament.Id);
        updated.Name.Should().Be("Copa Mundial 2026 - Fase Final");
        updated.Format.NumberOfGroups.Should().Be(NumberOfGroups);
        updated.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
        updated.Format.Type.Should().Be(TournamentType.NFL);
    }

    [Fact]
    public async Task Put_Mundial_ExpandingTo16GroupsIsPersisted()
    {
        var tournament = await CreateMundialAsync();

        var updateRequest = new UpdateTournamentDto(
            "Copa Mundial 2026 Ampliada",
            new TournamentFormatDto(TeamsPerGroup, 16, TournamentType.ROUND_ROBIN));

        await _client.PutAsJsonAsync($"/tournaments/{tournament.Id}", updateRequest);

        var fetched = await _client.GetFromJsonAsync<TournamentDto>($"/tournaments/{tournament.Id}");
        fetched.Should().NotBeNull();
        fetched!.Format.NumberOfGroups.Should().Be(16);
        fetched.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
    }

    [Theory]
    [InlineData("", NumberOfGroups)]
    [InlineData("Copa Mundial 2026", 0)]
    public async Task Put_Mundial_WithInvalidPayload_Returns400(string name, int numberOfGroups)
    {
        var tournament = await CreateMundialAsync();

        var updateRequest = new UpdateTournamentDto(
            name, new TournamentFormatDto(TeamsPerGroup, numberOfGroups, TournamentType.ROUND_ROBIN));

        var response = await _client.PutAsJsonAsync($"/tournaments/{tournament.Id}", updateRequest);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ---------------- PATCH ----------------

    [Fact]
    public async Task Patch_Mundial_NameOnly_KeepsEightGroupFormat()
    {
        var tournament = await CreateMundialAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}",
            new PatchTournamentDto(Name: "Mundial 2026"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Name.Should().Be("Mundial 2026");
        patched.Format.NumberOfGroups.Should().Be(NumberOfGroups);
        patched.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
        patched.Format.Type.Should().Be(TournamentType.ROUND_ROBIN);
    }

    [Fact]
    public async Task Patch_Mundial_TeamsPerGroupOnly_KeepsNameAndGroupCount()
    {
        var tournament = await CreateMundialAsync("Copa Mundial 2026");

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}",
            new PatchTournamentDto(Format: new PatchTournamentFormatDto(MaxTeamsPerGroup: 6)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Name.Should().Be("Copa Mundial 2026");
        patched.Format.MaxTeamsPerGroup.Should().Be(6);
        patched.Format.NumberOfGroups.Should().Be(NumberOfGroups);
        patched.Format.Type.Should().Be(TournamentType.ROUND_ROBIN);
    }

    [Fact]
    public async Task Patch_Mundial_SwitchesTypeToNfl_WithoutTouchingGroupLayout()
    {
        var tournament = await CreateMundialAsync();

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}",
            new PatchTournamentDto(Format: new PatchTournamentFormatDto(Type: TournamentType.NFL)));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var patched = await response.Content.ReadFromJsonAsync<TournamentDto>();
        patched!.Format.Type.Should().Be(TournamentType.NFL);
        patched.Format.NumberOfGroups.Should().Be(NumberOfGroups);
        patched.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
    }

    [Fact]
    public async Task Patch_Mundial_AppliedTwice_AccumulatesChanges()
    {
        var tournament = await CreateMundialAsync();

        await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}", new PatchTournamentDto(Name: "Mundial FIFA"));
        await _client.PatchAsJsonAsync(
            $"/tournaments/{tournament.Id}",
            new PatchTournamentDto(Format: new PatchTournamentFormatDto(NumberOfGroups: 12)));

        var fetched = await _client.GetFromJsonAsync<TournamentDto>($"/tournaments/{tournament.Id}");
        fetched!.Name.Should().Be("Mundial FIFA");
        fetched.Format.NumberOfGroups.Should().Be(12);
        fetched.Format.MaxTeamsPerGroup.Should().Be(TeamsPerGroup);
    }

    [Fact]
    public async Task Patch_Mundial_OnNonExistentId_Returns404()
    {
        var response = await _client.PatchAsJsonAsync(
            "/tournaments/mundial-inexistente", new PatchTournamentDto(Name: "Mundial 2026"));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ---------------- DELETE ----------------

    [Fact]
    public async Task Delete_Mundial_RemovesTournamentAndReturns404Afterwards()
    {
        var tournament = await CreateMundialAsync();
        await CreateAllGroupsAsync(tournament.Id);

        var response = await _client.DeleteAsync($"/tournaments/{tournament.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await response.Content.ReadAsByteArrayAsync()).Should().BeEmpty();

        var getResponse = await _client.GetAsync($"/tournaments/{tournament.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Mundial_DoesNotDeleteTheTeams()
    {
        var tournament = await CreateMundialAsync();
        await CreateAllTeamsAsync();

        await _client.DeleteAsync($"/tournaments/{tournament.Id}");

        var teams = await _client.GetFromJsonAsync<List<TeamDto>>("/teams");
        teams.Should().NotBeNull();
        teams!.Should().HaveCount(TotalTeams, "los equipos son un recurso independiente del torneo");
    }

    [Fact]
    public async Task Delete_Mundial_OnlyRemovesTheTargetTournament()
    {
        var mundial = await CreateMundialAsync("Copa Mundial 2026");
        var otro = await CreateMundialAsync("Copa Oro 2026");

        await _client.DeleteAsync($"/tournaments/{mundial.Id}");

        var remaining = await _client.GetFromJsonAsync<List<TournamentDto>>("/tournaments");
        remaining.Should().NotBeNull();
        remaining!.Should().ContainSingle();
        remaining[0].Id.Should().Be(otro.Id);
        remaining[0].Name.Should().Be("Copa Oro 2026");
    }

    [Fact]
    public async Task Delete_Mundial_Twice_ReturnsNotFoundOnSecondCall()
    {
        var tournament = await CreateMundialAsync();

        (await _client.DeleteAsync($"/tournaments/{tournament.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NoContent);

        (await _client.DeleteAsync($"/tournaments/{tournament.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_OneTeam_ShrinksTheRosterBelow32()
    {
        var teams = await CreateAllTeamsAsync();

        var response = await _client.DeleteAsync($"/teams/{teams[0].Id}");
        response.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var remaining = await _client.GetFromJsonAsync<List<TeamDto>>("/teams");
        remaining!.Should().HaveCount(TotalTeams - 1);
        remaining.Should().NotContain(t => t.Id == teams[0].Id);
    }
}