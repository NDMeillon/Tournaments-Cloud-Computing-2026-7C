using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Tournaments_V1.Contracts.Requests;
using Tournaments_V1.Contracts.Responses;

namespace Tournaments_V1.Tests.Endpoints;

public class TournamentEndpointsTests(WebApplicationFactory<Program> factory) 
    : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task CreateTournament_WithValidData_Returns201Created()
    {
        var request = new CreateTournamentRequest("torneo-1", "Torneo Apertura", DateTime.UtcNow.AddDays(10));

        var response = await _client.PostAsJsonAsync("/api/v1/tournaments", request);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        
        var body = await response.Content.ReadFromJsonAsync<TournamentResponse>();
        body.Should().NotBeNull();
        body!.Id.Should().Be("torneo-1");
    }

    [Fact]
    public async Task CreateTournament_WithInvalidId_Returns400BadRequest()
    {
        var request = new CreateTournamentRequest("id_con_caracter_invalido!", "Torneo", DateTime.UtcNow.AddDays(10));

        var response = await _client.PostAsJsonAsync("/api/v1/tournaments", request);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}