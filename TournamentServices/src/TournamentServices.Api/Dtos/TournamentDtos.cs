using System.Text.Json.Serialization;

namespace TournamentServices.Api.Dtos;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TournamentType
{
    ROUND_ROBIN,
    NFL
}

public record TournamentFormatDto(
    int MaxTeamsPerGroup,
    int NumberOfGroups,
    TournamentType Type
);

public record PatchTournamentFormatDto(
    int? MaxTeamsPerGroup = null,
    int? NumberOfGroups = null,
    TournamentType? Type = null
);

public record TournamentDto(
    string Id,
    string Name,
    TournamentFormatDto Format,
    List<GroupDto> Groups,
    List<MatchDto> Matches
);

public record CreateTournamentDto(
    string Name,
    TournamentFormatDto Format
);

public record UpdateTournamentDto(
    string Name,
    TournamentFormatDto Format
);

public record PatchTournamentDto(
    string? Name = null,
    PatchTournamentFormatDto? Format = null
);

// Legacy
public record CreateTournamentRequest(string Id, string Name, DateTime StartDate);
public record TournamentResponse(string Id, string Name, DateTime StartDate, List<GroupResponse> Groups);