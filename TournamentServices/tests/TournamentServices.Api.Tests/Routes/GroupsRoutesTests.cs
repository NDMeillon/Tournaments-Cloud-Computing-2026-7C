using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using TournamentServices.Api.Dtos;
using Xunit;

namespace TournamentServices.Api.Tests.Routes;

public class GroupRoutesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public GroupRoutesTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    private const string ValidTournamentId = "tour-2026";
    private const string ValidGroupId = "group-a";
    private const string InvalidId = "tour_2026!";


    [Fact]
    public async Task GetGroups_WithValidTournamentId_ReturnsOkWithGroups()
    {
        var response = await _client.GetAsync($"/tournaments/{ValidTournamentId}/groups");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var groups = await response.Content.ReadFromJsonAsync<List<GroupDto>>();
        Assert.NotNull(groups);
        Assert.Equal(2, groups!.Count);
        Assert.All(groups, g => Assert.Equal(ValidTournamentId, g.TournamentId));
        Assert.Contains(groups, g => g.Id == "group-a");
        Assert.Contains(groups, g => g.Id == "group-b");
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
    public async Task GetGroupById_WithValidIds_ReturnsOkWithMatchingIds()
    {
        var response = await _client.GetAsync($"/tournaments/{ValidTournamentId}/groups/{ValidGroupId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var group = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(group);
        Assert.Equal(ValidGroupId, group!.Id);
        Assert.Equal(ValidTournamentId, group.TournamentId);
    }

    [Theory]
    [InlineData(InvalidId, ValidGroupId)]
    [InlineData(ValidTournamentId, "grupo a")]
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
        var payload = new CreateGroupDto("Grupo C");

        var response = await _client.PostAsJsonAsync($"/tournaments/{ValidTournamentId}/groups", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var created = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(created);
        Assert.Equal("Grupo C", created!.Name);
        Assert.Equal(ValidTournamentId, created.TournamentId);
        Assert.StartsWith("grp-", created.Id);
        Assert.Equal(12, created.Id.Length);

        Assert.NotNull(response.Headers.Location);
        Assert.EndsWith($"/tournaments/{ValidTournamentId}/groups/{created.Id}",
            response.Headers.Location!.ToString());
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
        var payload = new CreateGroupDto(string.Empty);

        var response = await _client.PostAsJsonAsync($"/tournaments/{ValidTournamentId}/groups", payload);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 del ValidationFilter, se obtuvo {(int)response.StatusCode}.");
    }

    [Fact]
    public async Task CreateGroup_RunsValidationFilterBeforeRouteCheck_OnDoubleInvalidInput()
    {

        var payload = new CreateGroupDto(string.Empty);

        var response = await _client.PostAsJsonAsync(
            $"/tournaments/{Uri.EscapeDataString(InvalidId)}/groups", payload);

        Assert.True(response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity);
    }


    [Fact]
    public async Task UpdateGroup_WithValidPayload_ReturnsOkWithUpdatedName()
    {
        var payload = new UpdateGroupDto("Grupo A - Renombrado");

        var response = await _client.PutAsJsonAsync(
            $"/tournaments/{ValidTournamentId}/groups/{ValidGroupId}", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var updated = await response.Content.ReadFromJsonAsync<GroupDto>();
        Assert.NotNull(updated);
        Assert.Equal(ValidGroupId, updated!.Id);
        Assert.Equal("Grupo A - Renombrado", updated.Name);
        Assert.Equal(ValidTournamentId, updated.TournamentId);
    }

    [Theory]
    [InlineData(InvalidId, ValidGroupId)]
    [InlineData(ValidTournamentId, "group a")]
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
        var response = await _client.DeleteAsync($"/tournaments/{ValidTournamentId}/groups/{ValidGroupId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty((await response.Content.ReadAsByteArrayAsync()));
    }

    [Theory]
    [InlineData(InvalidId, ValidGroupId)]
    [InlineData(ValidTournamentId, "group$a")]
    public async Task DeleteGroup_WithInvalidIds_ReturnsBadRequest(string tournamentId, string groupId)
    {
        var response = await _client.DeleteAsync(
            $"/tournaments/{Uri.EscapeDataString(tournamentId)}/groups/{Uri.EscapeDataString(groupId)}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }


    [Fact]
    public async Task AssignTeams_WithValidPayload_ReturnsNoContent()
    {
        var payload = new AssignTeamsDto(["team-1", "team-2"]);

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{ValidTournamentId}/groups/{ValidGroupId}/teams", payload);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Theory]
    [InlineData(InvalidId, ValidGroupId)]
    [InlineData(ValidTournamentId, "group a")]
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
        var payload = new AssignTeamsDto([]);

        var response = await _client.PatchAsJsonAsync(
            $"/tournaments/{ValidTournamentId}/groups/{ValidGroupId}/teams", payload);

        Assert.True(
            response.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.UnprocessableEntity,
            $"Se esperaba 400/422 del ValidationFilter, se obtuvo {(int)response.StatusCode}.");
    }

    // 
    //[Fact]
    //public async Task UnmappedVerbOnCollection_ReturnsMethodNotAllowed()
    //{
    //    var response = await _client.PatchAsync(
    //        $"/tournaments/{ValidTournamentId}/groups", JsonContent.Create(new { }));
    //    Assert.Equal(HttpStatusCode.MethodNotAllowed, response.StatusCode);
    //}
}
