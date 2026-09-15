using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TeamRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";
    internal static readonly List<TeamDto> TeamsDb = [];

    public static void ResetDb() => TeamsDb.Clear();

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
        return TypedResults.Ok(TeamsDb.ToList());
    }

    private static Results<Ok<TeamDto>, BadRequest<string>, NotFound> GetTeamById(string teamId)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        var team = TeamsDb.FirstOrDefault(t => t.Id == teamId);
        return team is null ? TypedResults.NotFound() : TypedResults.Ok(team);
    }

    private static Results<Created<TeamDto>, BadRequest<string>> CreateTeam(CreateTeamDto request)
    {
        if (TeamsDb.Any(t => t.Name.Equals(request.Name, StringComparison.OrdinalIgnoreCase)))
        {
            return TypedResults.BadRequest("Duplicate team name.");
        }

        var generatedId = $"team-{Guid.NewGuid().ToString("N")[..8]}";
        var newTeam = new TeamDto(generatedId, request.Name);
        TeamsDb.Add(newTeam);

        return TypedResults.Created($"/teams/{newTeam.Id}", newTeam);
    }

    private static Results<Ok<TeamDto>, BadRequest<string>, NotFound> UpdateTeam(string teamId, UpdateTeamDto request)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        var index = TeamsDb.FindIndex(t => t.Id == teamId);
        if (index == -1)
        {
            return TypedResults.NotFound();
        }

        var updatedTeam = new TeamDto(teamId, request.Name);
        TeamsDb[index] = updatedTeam;

        return TypedResults.Ok(updatedTeam);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteTeam(string teamId)
    {
        if (!Regex.IsMatch(teamId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Team ID format.");
        }

        var team = TeamsDb.FirstOrDefault(t => t.Id == teamId);
        if (team is null)
        {
            return TypedResults.NotFound();
        }

        TeamsDb.Remove(team);
        return TypedResults.NoContent();
    }
}