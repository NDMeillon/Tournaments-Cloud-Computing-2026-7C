namespace TournamentServices.Domain;

public class Group
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string TournamentId { get; set; } = "";
    public Tournament? Tournament { get; set; }
    public List<Team> Teams { get; set; } = [];
}
