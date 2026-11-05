namespace TournamentServices.Domain;

public static class Standings
{
    public sealed record Row(string TeamId, string Name, int Wins, int GoalsFor, int GoalsAgainst);

    public static List<Row> Rank(
        IReadOnlyList<(string Id, string Name)> teams,
        IReadOnlyList<(string HomeId, int HomeGoals, string AwayId, int AwayGoals)> played)
    {
        var rows = teams.Select(team =>
        {
            var wins = 0;
            var goalsFor = 0;
            var goalsAgainst = 0;

            foreach (var match in played)
            {
                if (match.HomeId == team.Id)
                {
                    goalsFor += match.HomeGoals;
                    goalsAgainst += match.AwayGoals;
                    if (match.HomeGoals > match.AwayGoals)
                        wins++;
                }
                else if (match.AwayId == team.Id)
                {
                    goalsFor += match.AwayGoals;
                    goalsAgainst += match.HomeGoals;
                    if (match.AwayGoals > match.HomeGoals)
                        wins++;
                }
            }

            return new Row(team.Id, team.Name, wins, goalsFor, goalsAgainst);
        });

        return rows
            .OrderByDescending(row => row.Wins)
            .ThenByDescending(row => row.GoalsFor)
            .ThenBy(row => row.GoalsAgainst)
            .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
