namespace TournamentServices.Api.Dtos;

public record TournamentFormatDto(
    int MaxTeamsPerGroup,
    int NumberOfGroups
);

public record PatchTournamentFormatDto(
    int? MaxTeamsPerGroup = null,
    int? NumberOfGroups = null
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

public record StandingRowDto(string TeamId, string Name, int Wins, int GoalsFor, int GoalsAgainst);

public record GroupStandingDto(string GroupId, string GroupName, List<StandingRowDto> Table);

public record KnockoutRoundDto(int Round, List<MatchDto> Matches);

public record ChampionDto(string TeamId, string Name, List<KnockoutRoundDto> Rounds);

public record CreateTournamentRequest(string Id, string Name, DateTime StartDate);
public record TournamentResponse(string Id, string Name, DateTime StartDate, List<GroupResponse> Groups);
