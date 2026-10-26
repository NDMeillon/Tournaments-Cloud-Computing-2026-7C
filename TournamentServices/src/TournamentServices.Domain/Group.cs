namespace TournamentServices.Domain;

public class Group
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TournamentId { get; set; } = "";
    public Tournament? Tournament { get; set; }
    public List<Team> Teams { get; set; } = [];

    public bool ContainsTeam(string teamId) => Teams.Any(t => t.Id == teamId);

    public void AssignTeams(IReadOnlyCollection<Team> teams, int maxTeamsPerGroup)
    {
        if (teams.Select(t => t.Id).Distinct().Count() != teams.Count)
            throw new DomainException("Duplicate team IDs in request.");

        var duplicated = teams.FirstOrDefault(t => ContainsTeam(t.Id));
        if (duplicated is not null)
            throw new DomainException($"Team '{duplicated.Id}' is already in this group.");

        if (Teams.Count + teams.Count > maxTeamsPerGroup)
            throw new DomainException($"Group cannot exceed {maxTeamsPerGroup} teams.");

        Teams.AddRange(teams);
    }

    public void UnassignAllTeams() => Teams.Clear();
}