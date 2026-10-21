using TournamentServices.Api.Delegates;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class MatchRoutes
{
    public static RouteGroupBuilder MapMatchRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/matches")
                       .WithTags("Matches");

        group.MapGet("", GetMatches);
        group.MapGet("/{matchId}", GetMatchById);
        group.MapPost("", CreateMatch)
             .AddEndpointFilter<ValidationFilter<CreateMatchDto>>();
        group.MapPatch("/{matchId}/score", UpdateScore)
             .AddEndpointFilter<ValidationFilter<UpdateScoreDto>>();
        group.MapDelete("/{matchId}", DeleteMatch);

        return group;
    }

    private static async Task<IResult> GetMatches(string tournamentId, IMatchDelegate matches, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await matches.GetByTournamentAsync(tournamentId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> GetMatchById(
        string tournamentId,
        string matchId,
        IMatchDelegate matches,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(matchId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await matches.GetByIdAsync(tournamentId, matchId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> CreateMatch(
        string tournamentId,
        CreateMatchDto request,
        IMatchDelegate matches,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(
            await matches.CreateAsync(tournamentId, request, ct),
            match => TypedResults.Created($"/tournaments/{tournamentId}/matches/{match.Id}", match));
    }

    private static async Task<IResult> UpdateScore(
        string tournamentId,
        string matchId,
        UpdateScoreDto request,
        IMatchDelegate matches,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(matchId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await matches.UpdateScoreAsync(tournamentId, matchId, request, ct), TypedResults.Ok);
    }

    private static async Task<IResult> DeleteMatch(
        string tournamentId,
        string matchId,
        IMatchDelegate matches,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(matchId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await matches.DeleteAsync(tournamentId, matchId, ct), _ => TypedResults.NoContent());
    }
}
