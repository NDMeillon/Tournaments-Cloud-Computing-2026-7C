namespace Tournaments_V1.Contracts.Requests;

public record CreateTournamentRequest(string Id, string Name, DateTime StartDate);