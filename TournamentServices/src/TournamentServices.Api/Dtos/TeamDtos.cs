namespace TournamentServices.Api.Dtos;

public record TeamDto(string Id, string Name);

public record CreateTeamDto(string Name);

public record UpdateTeamDto(string Name);

// Legacy
public record AssignTeamsRequest(List<TeamDto> Teams);
public record TeamResponse(string Id, string Name);