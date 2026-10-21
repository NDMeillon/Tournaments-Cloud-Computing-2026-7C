using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Delegates;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TournamentRoutes
{
    public static RouteGroupBuilder MapTournamentRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tournaments")
                       .WithTags("Tournaments");

        group.MapGet("", GetAllTournaments);
        group.MapGet("/{tournamentId}", GetTournamentById);
        group.MapPost("", CreateTournament)
             .AddEndpointFilter<ValidationFilter<CreateTournamentDto>>();
        group.MapPut("/{tournamentId}", UpdateTournament)
             .AddEndpointFilter<ValidationFilter<UpdateTournamentDto>>();
        group.MapPatch("/{tournamentId}", PatchTournament);
        group.MapDelete("/{tournamentId}", DeleteTournament);

        return group;
    }

    private static async Task<Ok<List<TournamentDto>>> GetAllTournaments(ITournamentDelegate tournaments, CancellationToken ct) =>
        TypedResults.Ok(await tournaments.GetAllAsync(ct));

    private static async Task<IResult> GetTournamentById(string tournamentId, ITournamentDelegate tournaments, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await tournaments.GetByIdAsync(tournamentId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> CreateTournament(CreateTournamentDto request, ITournamentDelegate tournaments, CancellationToken ct)
    {
        return RouteSupport.From(
            await tournaments.CreateAsync(request, ct),
            tournament => TypedResults.Created($"/tournaments/{tournament.Id}", tournament));
    }

    private static async Task<IResult> UpdateTournament(
        string tournamentId,
        UpdateTournamentDto request,
        ITournamentDelegate tournaments,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await tournaments.UpdateAsync(tournamentId, request, ct), TypedResults.Ok);
    }

    private static async Task<IResult> PatchTournament(
        string tournamentId,
        PatchTournamentDto request,
        ITournamentDelegate tournaments,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await tournaments.PatchAsync(tournamentId, request, ct), TypedResults.Ok);
    }

    private static async Task<IResult> DeleteTournament(string tournamentId, ITournamentDelegate tournaments, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await tournaments.DeleteAsync(tournamentId, ct), _ => TypedResults.NoContent());
    }
}
