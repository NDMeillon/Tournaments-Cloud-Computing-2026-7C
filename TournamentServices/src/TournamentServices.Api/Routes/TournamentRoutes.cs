using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TournamentRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

    public static RouteGroupBuilder MapTournamentRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tournaments")
                       .WithTags("Tournaments");

        // Use "" so it matches "/api/v1/tournaments" without enforcing a trailing slash
        group.MapPost("", CreateTournament)
             .AddEndpointFilter<ValidationFilter<CreateTournamentRequest>>();

        group.MapGet("/{id}", GetTournamentById);

        group.MapPost("/{id}/groups", AssignGroups)
             .AddEndpointFilter<ValidationFilter<AssignGroupsRequest>>();

        group.MapPost("/{id}/groups/{groupId}/teams", AssignTeams)
             .AddEndpointFilter<ValidationFilter<AssignTeamsRequest>>();

        return group;
    }

    private static Results<Created<TournamentResponse>, BadRequest<string>> CreateTournament(
        CreateTournamentRequest request)
    {
        var stub = new TournamentResponse(
            request.Id,
            request.Name,
            request.StartDate,
            []
        );

        return TypedResults.Created($"/api/v1/tournaments/{request.Id}", stub);
    }

    private static Results<Ok<TournamentResponse>, BadRequest<string>, NotFound> GetTournamentById(
        string id)
    {
        if (!Regex.IsMatch(id, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stub = new TournamentResponse(
            id,
            "Copa Premier",
            DateTime.UtcNow.AddDays(7),
            []
        );

        return TypedResults.Ok(stub);
    }

    private static Results<Ok<TournamentResponse>, BadRequest<string>, NotFound> AssignGroups(
        string id,
        AssignGroupsRequest request)
    {
        if (!Regex.IsMatch(id, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var groupResponses = request.Groups
            .Select(g => new GroupResponse(g.Id, g.Name, []))
            .ToList();

        var stub = new TournamentResponse(
            id,
            "Copa Premier",
            DateTime.UtcNow.AddDays(7),
            groupResponses
        );

        return TypedResults.Ok(stub);
    }

    private static Results<Ok<GroupResponse>, BadRequest<string>, NotFound> AssignTeams(
        string id,
        string groupId,
        AssignTeamsRequest request)
    {
        if (!Regex.IsMatch(id, IdPattern) || !Regex.IsMatch(groupId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format in route parameters.");
        }

        var teamResponses = request.Teams
            .Select(t => new TeamResponse(t.Id, t.Name))
            .ToList();

        var stub = new GroupResponse(
            groupId,
            "Grupo A",
            teamResponses
        );

        return TypedResults.Ok(stub);
    }
}