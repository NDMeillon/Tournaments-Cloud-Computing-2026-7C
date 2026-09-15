using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TeamRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

    public static RouteGroupBuilder MapTeamRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/teams")
                       .WithTags("Teams");

        group.MapGet("", GetAllTeams);

        group.MapGet("/{teamId}", GetTeamById);

        group.MapPost("", CreateTeam)
             .AddEndpointFilter<ValidationFilter<CreateTeamDto>>();

        group.MapPut("/{teamId}", UpdateTeam)
             .AddEndpointFilter<ValidationFilter<UpdateTeamDto>>();

        group.MapDelete("/{teamId}", DeleteTeam);

        return group;
    }

    private static Ok<List<TeamDto>> GetAllTeams()
    {
        var stubs = new List<TeamDto>
        {
            new("team-1", "Tigres"),
            new("team-2", "Rayados")
        };
        return TypedResults.Ok(stubs);
    }

    private static Results<Ok<TeamDto>, BadRequest<string>, NotFound> GetTeamById(string teamId)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        var stub = new TeamDto(teamId, "Tigres");
        return TypedResults.Ok(stub);
    }

    private static Created<TeamDto> CreateTeam(CreateTeamDto request)
    {
        var generatedId = $"team-{Guid.NewGuid().ToString("N")[..8]}";
        var stub = new TeamDto(generatedId, request.Name);

        return TypedResults.Created($"/teams/{stub.Id}", stub);
    }

    private static Results<Ok<TeamDto>, BadRequest<string>, NotFound> UpdateTeam(string teamId, UpdateTeamDto request)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        var stub = new TeamDto(teamId, request.Name);
        return TypedResults.Ok(stub);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteTeam(string teamId)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        return TypedResults.NoContent();
    }
}