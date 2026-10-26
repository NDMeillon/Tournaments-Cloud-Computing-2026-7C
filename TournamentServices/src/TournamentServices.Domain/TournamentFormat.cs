namespace TournamentServices.Domain;

public class TournamentFormat
{
    public int MaxTeamsPerGroup { get; set; }
    public int NumberOfGroups { get; set; }
    public TournamentType Type { get; set; }

    public TournamentFormat() { }

    public TournamentFormat(int maxTeamsPerGroup, int numberOfGroups, TournamentType type)
    {
        if (maxTeamsPerGroup <= 0)
            throw new DomainException("MaxTeamsPerGroup must be greater than 0.");
        if (numberOfGroups <= 0)
            throw new DomainException("NumberOfGroups must be greater than 0.");

        MaxTeamsPerGroup = maxTeamsPerGroup;
        NumberOfGroups = numberOfGroups;
        Type = type;
    }
}