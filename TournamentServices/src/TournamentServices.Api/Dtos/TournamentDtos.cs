namespace TournamentServices.Api.Dtos;

public record CreateTournamentRequest(string Id, string Name, DateTime StartDate);
public record TournamentResponse(string Id, string Name, DateTime StartDate, List<GroupResponse> Groups);