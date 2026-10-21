using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Delegates;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TeamRoutes
{
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

    private static async Task<Ok<List<TeamDto>>> GetAllTeams(ITeamDelegate teams, CancellationToken ct) =>
        TypedResults.Ok(await teams.GetAllAsync(ct));

    private static async Task<IResult> GetTeamById(string teamId, ITeamDelegate teams, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(teamId))
            return TypedResults.BadRequest("Invalid Team ID format.");

        return RouteSupport.From(await teams.GetByIdAsync(teamId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> CreateTeam(CreateTeamDto request, ITeamDelegate teams, CancellationToken ct)
    {
        return RouteSupport.From(
            await teams.CreateAsync(request, ct),
            team => TypedResults.Created($"/teams/{team.Id}", team));
    }

    private static async Task<IResult> UpdateTeam(string teamId, UpdateTeamDto request, ITeamDelegate teams, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(teamId))
            return TypedResults.BadRequest("Invalid Team ID format.");

        return RouteSupport.From(await teams.UpdateAsync(teamId, request, ct), TypedResults.Ok);
    }

    private static async Task<IResult> DeleteTeam(string teamId, ITeamDelegate teams, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(teamId))
            return TypedResults.BadRequest("Invalid Team ID format.");

        return RouteSupport.From(await teams.DeleteAsync(teamId, ct), _ => TypedResults.NoContent());
    }
}
