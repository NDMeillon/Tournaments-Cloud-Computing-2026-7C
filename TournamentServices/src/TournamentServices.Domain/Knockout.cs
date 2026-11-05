namespace TournamentServices.Domain;

public static class KnockoutPairing
{
    public static IReadOnlyList<(string HomeId, string? AwayId)> Pair(IReadOnlyList<string> seeds)
    {
        if (seeds.Count <= 1)
            return [];

        var size = 1;
        while (size < seeds.Count)
            size *= 2;

        var pairs = new List<(string HomeId, string? AwayId)>();
        var half = size / 2;
        for (var slot = 0; slot < half; slot++)
        {
            var home = slot < seeds.Count ? seeds[slot] : null;
            var awayIndex = size - 1 - slot;
            var away = awayIndex < seeds.Count ? seeds[awayIndex] : null;
            if (home is null && away is null)
                continue;
            if (home is null)
                pairs.Add((away!, null));
            else
                pairs.Add((home, away));
        }

        return pairs;
    }
}

public static class KnockoutScore
{
    public static (int Home, int Away) Play(Random random)
    {
        var home = random.Next(0, 6);
        var away = random.Next(0, 6);
        if (home == away)
        {
            if (random.Next(2) == 0)
                home++;
            else
                away++;
        }

        return (home, away);
    }
}
