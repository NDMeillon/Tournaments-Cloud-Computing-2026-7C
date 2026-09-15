using System.Text.Json.Serialization;

namespace TournamentServices.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MatchWinner
{
    HOME,
    VISITOR
}

public record ScoreDto(
    int HomeTeamScore,
    int VisitorTeamScore
);

public record MatchDto(
    string Id,
    string TournamentId,
    string? GroupId,
    string HomeTeamId,
    string VisitorTeamId,
    TeamDto? HomeTeam,
    TeamDto? VisitorTeam,
    ScoreDto Score,
    MatchWinner? Winner,
    bool IsCompleted
);

public record CreateMatchDto(
    string? GroupId,
    string HomeTeamId,
    string VisitorTeamId
);

public record UpdateScoreDto(
    int HomeTeamScore,
    int VisitorTeamScore
);