namespace Tournaments_V1.Contracts.Responses;

public record TournamentResponse(string Id, string Name, DateTime StartDate, List<GroupResponse> Groups);
public record GroupResponse(string Id, string Name, List<TeamResponse> Teams);
public record TeamResponse(string Id, string Name);