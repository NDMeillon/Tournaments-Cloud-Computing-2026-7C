using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using Tournaments_V1.Common.Filters;
using Tournaments_V1.Contracts.Requests;
using Tournaments_V1.Contracts.Responses;

namespace Tournaments_V1.Endpoints;

public static class TournamentEndpoints
{
    public static RouteGroupBuilder MapTournamentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tournaments")
                       .WithTags("Tournaments");

        group.MapPost("/", CreateTournament)
             .AddEndpointFilter<ValidationFilter<CreateTournamentRequest>>();

        group.MapGet("/{id}", GetTournamentById);

        group.MapPost("/{id}/groups", AssignGroups)
             .AddEndpointFilter<ValidationFilter<AssignGroupsRequest>>();

        group.MapPost("/{id}/groups/{groupId}/teams", AssignTeams)
             .AddEndpointFilter<ValidationFilter<AssignTeamsRequest>>();

        return group;
    }

    private static Results<Created<TournamentResponse>, BadRequest<string>> CreateTournament(CreateTournamentRequest request)
    {
        var stub = new TournamentResponse(request.Id, request.Name, request.StartDate, []);
        return TypedResults.Created($"/api/v1/tournaments/{stub.Id}", stub);
    }

    private static Results<Ok<TournamentResponse>, NotFound, BadRequest<string>> GetTournamentById(string id)
    {
        if (!Regex.IsMatch(id, @"^[A-Za-z0-9\-]+$"))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stub = new TournamentResponse(id, "Copa Premier", DateTime.UtcNow.AddDays(7), []);
        return TypedResults.Ok(stub);
    }

    private static Results<Ok<TournamentResponse>, NotFound, BadRequest<string>> AssignGroups(string id, AssignGroupsRequest request)
    {
        if (!Regex.IsMatch(id, @"^[A-Za-z0-9\-]+$"))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var groupsResponse = request.Groups.Select(g => new GroupResponse(g.Id, g.Name, [])).ToList();
        var stub = new TournamentResponse(id, "Copa Premier", DateTime.UtcNow.AddDays(7), groupsResponse);
        
        return TypedResults.Ok(stub);
    }

    private static Results<Ok<GroupResponse>, NotFound, BadRequest<string>> AssignTeams(string id, string groupId, AssignTeamsRequest request)
    {
        if (!Regex.IsMatch(id, @"^[A-Za-z0-9\-]+$") || !Regex.IsMatch(groupId, @"^[A-Za-z0-9\-]+$"))
        {
            return TypedResults.BadRequest("Invalid ID format in route parameters.");
        }

        var teamsResponse = request.Teams.Select(t => new TeamResponse(t.Id, t.Name)).ToList();
        var stub = new GroupResponse(groupId, "Grupo A", teamsResponse);

        return TypedResults.Ok(stub);
    }
}