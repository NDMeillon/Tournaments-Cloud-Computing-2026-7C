using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class MatchRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

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

    private static Results<Ok<List<MatchDto>>, BadRequest<string>> GetMatches(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stubs = new List<MatchDto>
        {
            CreateMatchStub("match-1", tournamentId, "team-1", "team-2")
        };

        return TypedResults.Ok(stubs);
    }

    private static Results<Ok<MatchDto>, BadRequest<string>, NotFound> GetMatchById(string tournamentId, string matchId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(matchId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        var stub = CreateMatchStub(matchId, tournamentId, "team-1", "team-2");
        return TypedResults.Ok(stub);
    }

    private static Results<Created<MatchDto>, BadRequest<string>> CreateMatch(string tournamentId, CreateMatchDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var generatedId = $"mtc-{Guid.NewGuid().ToString("N")[..8]}";
        var stub = CreateMatchStub(generatedId, tournamentId, request.HomeTeamId, request.VisitorTeamId, request.GroupId);

        return TypedResults.Created($"/tournaments/{tournamentId}/matches/{stub.Id}", stub);
    }

    private static Results<Ok<MatchDto>, BadRequest<string>, NotFound> UpdateScore(
        string tournamentId,
        string matchId,
        UpdateScoreDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(matchId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        var winner = request.HomeTeamScore > request.VisitorTeamScore
            ? MatchWinner.HOME
            : MatchWinner.VISITOR;

        var stub = new MatchDto(
            Id: matchId,
            TournamentId: tournamentId,
            GroupId: "group-a",
            HomeTeamId: "team-1",
            VisitorTeamId: "team-2",
            HomeTeam: new TeamDto("team-1", "Tigres"),
            VisitorTeam: new TeamDto("team-2", "Rayados"),
            Score: new ScoreDto(request.HomeTeamScore, request.VisitorTeamScore),
            Winner: winner,
            IsCompleted: true
        );

        return TypedResults.Ok(stub);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteMatch(string tournamentId, string matchId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(matchId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        return TypedResults.NoContent();
    }

    private static MatchDto CreateMatchStub(
        string matchId,
        string tournamentId,
        string homeTeamId,
        string visitorTeamId,
        string? groupId = null) =>
        new(
            Id: matchId,
            TournamentId: tournamentId,
            GroupId: groupId,
            HomeTeamId: homeTeamId,
            VisitorTeamId: visitorTeamId,
            HomeTeam: new TeamDto(homeTeamId, "Equipo Local"),
            VisitorTeam: new TeamDto(visitorTeamId, "Equipo Visitante"),
            Score: new ScoreDto(0, 0),
            Winner: null,
            IsCompleted: false
        );
}