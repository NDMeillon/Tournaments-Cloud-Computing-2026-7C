using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class TournamentRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";
    internal static readonly List<TournamentDto> TournamentsDb = [];

    public static void ResetDb() => TournamentsDb.Clear();

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
        group.MapPatch("/{tournamentId}", PatchTournament)
             .AddEndpointFilter<ValidationFilter<PatchTournamentDto>>();
        group.MapDelete("/{tournamentId}", DeleteTournament);

        return group;
    }

    private static Ok<List<TournamentDto>> GetAllTournaments()
    {
        return TypedResults.Ok(TournamentsDb.ToList());
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> GetTournamentById(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var tournament = TournamentsDb.FirstOrDefault(t => t.Id == tournamentId);
        return tournament is null ? TypedResults.NotFound() : TypedResults.Ok(tournament);
    }

    private static Created<TournamentDto> CreateTournament(CreateTournamentDto request)
    {
        var generatedId = $"trn-{Guid.NewGuid().ToString("N")[..8]}";
        var tournament = new TournamentDto(
            Id: generatedId,
            Name: request.Name,
            Format: request.Format,
            Groups: [],
            Matches: []
        );

        TournamentsDb.Add(tournament);
        return TypedResults.Created($"/tournaments/{tournament.Id}", tournament);
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> UpdateTournament(
        string tournamentId,
        UpdateTournamentDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var index = TournamentsDb.FindIndex(t => t.Id == tournamentId);
        if (index == -1)
        {
            return TypedResults.NotFound();
        }

        var existing = TournamentsDb[index];
        var updated = existing with
        {
            Name = request.Name,
            Format = request.Format
        };

        TournamentsDb[index] = updated;
        return TypedResults.Ok(updated);
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> PatchTournament(
        string tournamentId,
        PatchTournamentDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var index = TournamentsDb.FindIndex(t => t.Id == tournamentId);
        if (index == -1)
        {
            return TypedResults.NotFound();
        }

        var existing = TournamentsDb[index];
        var newName = request.Name ?? existing.Name;
        
        var newFormat = existing.Format;
        if (request.Format is not null)
        {
            newFormat = new TournamentFormatDto(
                request.Format.MaxTeamsPerGroup ?? existing.Format.MaxTeamsPerGroup,
                request.Format.NumberOfGroups ?? existing.Format.NumberOfGroups,
                request.Format.Type ?? existing.Format.Type
            );
        }

        var patched = existing with
        {
            Name = newName,
            Format = newFormat
        };

        TournamentsDb[index] = patched;
        return TypedResults.Ok(patched);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteTournament(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var tournament = TournamentsDb.FirstOrDefault(t => t.Id == tournamentId);
        if (tournament is null)
        {
            return TypedResults.NotFound();
        }

        TournamentsDb.Remove(tournament);
        return TypedResults.NoContent();
    }
}