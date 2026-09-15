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
        var stubs = new List<TournamentDto>
        {
            CreateStub("torneo-1", "Copa Premier", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN)),
            CreateStub("torneo-2", "Liga Clausura", new TournamentFormatDto(6, 4, TournamentType.NFL))
        };

        return TypedResults.Ok(stubs);
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> GetTournamentById(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stub = CreateStub(tournamentId, "Copa Premier", new TournamentFormatDto(4, 2, TournamentType.ROUND_ROBIN));
        return TypedResults.Ok(stub);
    }

    private static Created<TournamentDto> CreateTournament(CreateTournamentDto request)
    {
        var generatedId = $"trn-{Guid.NewGuid().ToString("N")[..8]}";
        var stub = CreateStub(generatedId, request.Name, request.Format);

        return TypedResults.Created($"/tournaments/{stub.Id}", stub);
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> UpdateTournament(
        string tournamentId,
        UpdateTournamentDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stub = CreateStub(tournamentId, request.Name, request.Format);
        return TypedResults.Ok(stub);
    }

    private static Results<Ok<TournamentDto>, BadRequest<string>, NotFound> PatchTournament(
        string tournamentId,
        PatchTournamentDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var resolvedFormat = new TournamentFormatDto(
            request.Format?.MaxTeamsPerGroup ?? 4,
            request.Format?.NumberOfGroups ?? 2,
            request.Format?.Type ?? TournamentType.ROUND_ROBIN
        );

        var stub = CreateStub(tournamentId, request.Name ?? "Torneo Parcial", resolvedFormat);
        return TypedResults.Ok(stub);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteTournament(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        return TypedResults.NoContent();
    }

    private static TournamentDto CreateStub(string id, string name, TournamentFormatDto format) =>
        new(
            Id: id,
            Name: name,
            Format: format,
            Groups: [],
            Matches: []
        );
}