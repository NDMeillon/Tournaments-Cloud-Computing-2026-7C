namespace TournamentServices.Domain;

public class Tournament
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public TournamentFormat Format { get; set; } = new();
    public List<Group> Groups { get; set; } = [];
    public List<Match> Matches { get; set; } = [];

    public bool HasGroupNamed(string name, string? excludeGroupId = null) =>
        Groups.Any(g => g.Id != excludeGroupId &&
                        string.Equals(g.Name, name, StringComparison.OrdinalIgnoreCase));

    public bool IsTeamInTournament(string teamId) =>
        Groups.Any(g => g.Teams.Any(t => t.Id == teamId));

    public Group AddGroup(string id, string name)
    {
        if (HasGroupNamed(name))
            throw new DomainException($"Group '{name}' already exists in this tournament.");
        if (Groups.Count >= Format.NumberOfGroups)
            throw new DomainException($"Tournament allows max {Format.NumberOfGroups} groups.");

        var group = new Group { Id = id, Name = name, TournamentId = Id, Tournament = this };
        Groups.Add(group);
        return group;
    }

    public void AssignTeamsToGroup(string groupId, IReadOnlyCollection<Team> teams)
    {
        var group = Groups.FirstOrDefault(g => g.Id == groupId)
            ?? throw new DomainException($"Group '{groupId}' not found in tournament.");

        var alreadyInOtherGroup = teams.FirstOrDefault(t =>
            Groups.Any(g => g.Id != groupId && g.Teams.Any(x => x.Id == t.Id)));
        if (alreadyInOtherGroup is not null)
            throw new DomainException($"Team '{alreadyInOtherGroup.Id}' is already assigned to another group.");

        group.AssignTeams(teams, Format.MaxTeamsPerGroup);
    }
}