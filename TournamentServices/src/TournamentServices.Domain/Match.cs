namespace TournamentServices.Domain;

public class Match
{
    public string Id { get; set; } = "";
    public string TournamentId { get; set; } = "";
    public string? GroupId { get; set; }
    public string HomeTeamId { get; set; } = "";
    public string VisitorTeamId { get; set; } = "";
    public Score Score { get; set; } = new();
    public bool IsCompleted { get; set; }

    public Team? HomeTeam { get; set; }
    public Team? VisitorTeam { get; set; }
    public Group? Group { get; set; }
    public Tournament? Tournament { get; set; }

    public MatchWinner? Winner => IsCompleted ? Score.GetWinner() : null;

    public static Match Create(string id, Tournament tournament, string homeTeamId,
                               string visitorTeamId, string? groupId = null)
    {
        if (homeTeamId == visitorTeamId)
            throw new DomainException("Home and visitor team must be different.");
        if (!tournament.IsTeamInTournament(homeTeamId) || !tournament.IsTeamInTournament(visitorTeamId))
            throw new DomainException("Both teams must belong to the tournament.");

        if (groupId is not null)
        {
            var group = tournament.Groups.FirstOrDefault(g => g.Id == groupId)
                ?? throw new DomainException($"Group '{groupId}' not found in tournament.");
            if (!group.ContainsTeam(homeTeamId) || !group.ContainsTeam(visitorTeamId))
                throw new DomainException("Both teams must be in the same group.");
        }

        return new Match
        {
            Id = id,
            TournamentId = tournament.Id,
            GroupId = groupId,
            HomeTeamId = homeTeamId,
            VisitorTeamId = visitorTeamId
        };
    }

    public void RecordScore(int homeScore, int visitorScore)
    {
        Score = new Score(homeScore, visitorScore); // valida negativos con el nombre correcto
        IsCompleted = true;
    }
}