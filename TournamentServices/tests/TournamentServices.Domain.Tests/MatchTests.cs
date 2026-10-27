using FluentAssertions;
using TournamentServices.Domain;
using Xunit;

namespace TournamentServices.Domain.Tests;

public class MatchTests
{
    [Fact]
    public void RecordScore_HomeWins_SetsWinnerAndCompleted()
    {
        var match = new Match();

        match.RecordScore(2, 1);

        match.IsCompleted.Should().BeTrue();
        match.Winner.Should().Be(MatchWinner.HOME);
    }

    [Fact]
    public void RecordScore_VisitorWins_SetsVisitor()
    {
        var match = new Match();

        match.RecordScore(0, 3);

        match.Winner.Should().Be(MatchWinner.VISITOR);
    }

    [Fact]
    public void RecordScore_Tie_LeavesWinnerNull()
    {
        var match = new Match();

        match.RecordScore(1, 1);

        match.IsCompleted.Should().BeTrue();
        match.Winner.Should().BeNull();
    }

    [Fact]
    public void RecordScore_Negative_Throws()
    {
        var match = new Match();

        var act = () => match.RecordScore(-1, 0);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Winner_BeforeScore_IsNull()
    {
        var match = new Match { Score = new Score(4, 0), IsCompleted = false };

        match.Winner.Should().BeNull();
    }
}
