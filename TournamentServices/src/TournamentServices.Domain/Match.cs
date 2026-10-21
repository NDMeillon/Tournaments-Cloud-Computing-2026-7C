namespace TournamentServices.Domain;

public class Match
{
    public string Id { get; set; } = "";
    public string TournamentId { get; set; } = "";
    public string? GroupId { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string VisitorTeamId { get; set; } = "";
    public int HomeScore { get; set; }
    public int VisitorScore { get; set; }
    public bool IsCompleted { get; set; }

    public Team? HomeTeam { get; set; }
    public Team? VisitorTeam { get; set; }
    public Group? Group { get; set; }
    public Tournament? Tournament { get; set; }

    public MatchWinner? Winner =>
        !IsCompleted ? null
        : HomeScore > VisitorScore ? MatchWinner.HOME
        : VisitorScore > HomeScore ? MatchWinner.VISITOR
        : null;

    public void RecordScore(int homeScore, int visitorScore)
    {
        if (homeScore < 0 || visitorScore < 0)
            throw new ArgumentOutOfRangeException(nameof(homeScore), "Scores cannot be negative.");

        HomeScore = homeScore;
        VisitorScore = visitorScore;
        IsCompleted = true;
    }
}
