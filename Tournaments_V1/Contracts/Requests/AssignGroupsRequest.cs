namespace Tournaments_V1.Contracts.Requests;

public record AssignGroupsRequest(List<GroupDto> Groups);
public record GroupDto(string Id, string Name);