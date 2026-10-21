namespace TournamentServices.Domain;

public class Tournament
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int MaxTeamsPerGroup { get; set; }
    public int NumberOfGroups { get; set; }
    public TournamentType Type { get; set; }

    public List<Group> Groups { get; set; } = [];
    public List<Match> Matches { get; set; } = [];
}
