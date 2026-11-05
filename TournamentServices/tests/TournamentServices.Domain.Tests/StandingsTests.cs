using FluentAssertions;
using TournamentServices.Domain;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class StandingsTests
{
    [Fact]
    public void Rank_OrdersByWinsGoalsForGoalsAgainstThenName()
    {
        var teams = new (string Id, string Name)[]
        {
            ("zeta", "Zeta"),
            ("alfa", "Alfa"),
            ("bravo", "Bravo"),
            ("atlas", "Atlas"),
            ("boca", "Boca")
        };

        var played = new (string HomeId, int HomeGoals, string AwayId, int AwayGoals)[]
        {
            ("atlas", 2, "boca", 0),
            ("atlas", 1, "bravo", 0),
            ("boca", 3, "bravo", 1),
            ("alfa", 1, "zeta", 1)
        };

        var table = Standings.Rank(teams, played);

        table.Select(row => row.Name).Should().Equal("Atlas", "Boca", "Alfa", "Zeta", "Bravo");
        table[0].Wins.Should().Be(2);
        table[1].Wins.Should().Be(1);
        table[1].GoalsFor.Should().Be(3);
        table[2].Wins.Should().Be(0);
        table[2].GoalsAgainst.Should().Be(1);
        table[4].GoalsAgainst.Should().Be(4);
    }

    [Fact]
    public void Pair_GivesByesToTheBestSeeds()
    {
        var pairs = KnockoutPairing.Pair(["a", "b", "c", "d", "e", "f"]);

        pairs.Should().HaveCount(4);
        pairs[0].Should().Be(("a", null));
        pairs[1].Should().Be(("b", null));
        pairs[2].Should().Be(("c", "f"));
        pairs[3].Should().Be(("d", "e"));
    }

    [Fact]
    public void Pair_OfFour_IsBestAgainstWorst()
    {
        var pairs = KnockoutPairing.Pair(["1", "2", "3", "4"]);

        pairs.Should().Equal([("1", "4"), ("2", "3")]);
    }

    [Fact]
    public void Play_AlwaysProducesAWinner()
    {
        var random = new Random(7);

        for (var i = 0; i < 200; i++)
        {
            var (home, away) = KnockoutScore.Play(random);
            home.Should().BeGreaterThanOrEqualTo(0);
            away.Should().BeGreaterThanOrEqualTo(0);
            home.Should().NotBe(away);
        }
    }
}
