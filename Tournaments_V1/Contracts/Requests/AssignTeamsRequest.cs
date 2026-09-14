namespace Tournaments_V1.Contracts.Requests;

public record AssignTeamsRequest(List<TeamDto> Teams);
public record TeamDto(string Id, string Name);