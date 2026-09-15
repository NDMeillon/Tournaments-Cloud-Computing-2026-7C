namespace TournamentServices.Api.Dtos;

public record AssignTeamsRequest(List<TeamDto> Teams);
public record TeamDto(string Id, string Name);
public record TeamResponse(string Id, string Name);