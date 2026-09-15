namespace TournamentServices.Api.Dtos;

public record GroupDto(
    string Id,
    string Name,
    string TournamentId,
    List<TeamDto> Teams
);

public record CreateGroupDto(string Name);

public record UpdateGroupDto(string Name);

public record AssignTeamsDto(List<string> TeamIds);

// Legacy
public record AssignGroupsRequest(List<LegacyGroupDto> Groups);
public record LegacyGroupDto(string Id, string Name);
public record GroupResponse(string Id, string Name, List<TeamResponse> Teams);