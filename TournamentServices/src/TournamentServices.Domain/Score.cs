namespace TournamentServices.Domain;

public class Score
{
    public int HomeTeamScore { get; set; }
    public int VisitorTeamScore { get; set; }

    public Score() { }

    public Score(int homeTeamScore, int visitorTeamScore)
    {
        if (homeTeamScore < 0)
            throw new ArgumentOutOfRangeException(nameof(homeTeamScore), "Score cannot be negative.");
        if (visitorTeamScore < 0)
            throw new ArgumentOutOfRangeException(nameof(visitorTeamScore), "Score cannot be negative.");

        HomeTeamScore = homeTeamScore;
        VisitorTeamScore = visitorTeamScore;
    }

    public MatchWinner? GetWinner() =>
        HomeTeamScore > VisitorTeamScore ? MatchWinner.HOME
        : VisitorTeamScore > HomeTeamScore ? MatchWinner.VISITOR
        : null; // empate
}