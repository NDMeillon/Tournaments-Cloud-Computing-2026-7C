namespace TournamentServices.Api.Dtos;

public record AssignGroupsRequest(List<GroupDto> Groups);
public record GroupDto(string Id, string Name);
public record GroupResponse(string Id, string Name, List<TeamResponse> Teams);