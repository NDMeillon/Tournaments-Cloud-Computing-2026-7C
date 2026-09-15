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

    [Fact]
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
    }

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
}