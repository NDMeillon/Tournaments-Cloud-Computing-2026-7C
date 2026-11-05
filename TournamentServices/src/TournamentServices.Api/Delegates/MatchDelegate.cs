using TournamentServices.Api.Dtos;
using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public class MatchDelegate(
    IMatchRepository matches,
    ITournamentRepository tournaments,
    IGroupRepository groups,
    ITeamRepository teams,
    IUnitOfWork uow) : IMatchDelegate
{
    public async Task<OperationResult<List<MatchDto>>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
            return OperationResult<List<MatchDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        return OperationResult<List<MatchDto>>.Ok(await matches.GetByTournamentAsync(tournamentId, ct));
    }

    public async Task<OperationResult<MatchDto>> GetByIdAsync(string tournamentId, string matchId, CancellationToken ct = default)
    {
        var match = await matches.GetByIdAsync(matchId, ct);
        if (match is null || match.TournamentId != tournamentId)
            return OperationResult<MatchDto>.NotFound($"Match '{matchId}' not found in tournament '{tournamentId}'.");

        return OperationResult<MatchDto>.Ok(match);
    }

    public async Task<OperationResult<MatchDto>> CreateAsync(string tournamentId, CreateMatchDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
            return OperationResult<MatchDto>.NotFound($"Tournament '{tournamentId}' not found.");

        if (dto.HomeTeamId == dto.VisitorTeamId)
            return OperationResult<MatchDto>.Conflict("Home team and visitor team must be different.");

        if (await teams.GetByIdAsync(dto.HomeTeamId, ct) is null || await teams.GetByIdAsync(dto.VisitorTeamId, ct) is null)
            return OperationResult<MatchDto>.Conflict("Both teams must belong to this tournament.");

        var tournamentGroups = await groups.GetByTournamentAsync(tournamentId, ct);

        if (!string.IsNullOrWhiteSpace(dto.GroupId))
        {
            var group = tournamentGroups.FirstOrDefault(g => g.Id == dto.GroupId);
            if (group is null)
                return OperationResult<MatchDto>.NotFound($"Group '{dto.GroupId}' not found in tournament '{tournamentId}'.");

            var memberIds = group.Teams.Select(t => t.Id).ToHashSet();
            if (!memberIds.Contains(dto.HomeTeamId) || !memberIds.Contains(dto.VisitorTeamId))
                return OperationResult<MatchDto>.Conflict(
                    $"Both teams must belong to group '{dto.GroupId}'.");
        }
        else
        {
            var memberIds = tournamentGroups.SelectMany(g => g.Teams).Select(t => t.Id).ToHashSet();
            if (!memberIds.Contains(dto.HomeTeamId) || !memberIds.Contains(dto.VisitorTeamId))
                return OperationResult<MatchDto>.Conflict("Both teams must belong to this tournament.");
        }

        var match = BuildMatch(tournamentId, dto.GroupId, dto.HomeTeamId, dto.VisitorTeamId);
        var created = await matches.AddAsync(match, ct);
        await uow.CommitAsync(ct);

        return OperationResult<MatchDto>.Ok(created);
    }

    public async Task<OperationResult<MatchDto>> UpdateScoreAsync(string tournamentId, string matchId, UpdateScoreDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var match = await matches.GetByIdAsync(matchId, ct);
        if (match is null || match.TournamentId != tournamentId)
            return OperationResult<MatchDto>.NotFound($"Match '{matchId}' not found in tournament '{tournamentId}'.");

        if (dto.HomeTeamScore < 0 || dto.VisitorTeamScore < 0)
            return OperationResult<MatchDto>.Invalid("Scores cannot be negative.");

        MatchWinner? winner = dto.HomeTeamScore == dto.VisitorTeamScore
            ? null
            : dto.HomeTeamScore > dto.VisitorTeamScore ? MatchWinner.HOME : MatchWinner.VISITOR;

        var updated = match with
        {
            Score = new ScoreDto(dto.HomeTeamScore, dto.VisitorTeamScore),
            Winner = winner,
            IsCompleted = true
        };
        var saved = await matches.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<MatchDto>.Ok(saved!);
    }

    public async Task<OperationResult<bool>> DeleteAsync(string tournamentId, string matchId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var match = await matches.GetByIdAsync(matchId, ct);
        if (match is null || match.TournamentId != tournamentId)
            return OperationResult<bool>.NotFound($"Match '{matchId}' not found in tournament '{tournamentId}'.");

        await matches.RemoveAsync(matchId, ct);
        await uow.CommitAsync(ct);

        return OperationResult<bool>.Ok(true);
    }

    public async Task<OperationResult<List<MatchDto>>> GenerateRoundRobinAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var scope = await uow.BeginAsync(ct);

        try
        {
            if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
                return OperationResult<List<MatchDto>>.NotFound($"Tournament '{tournamentId}' not found.");

            var already = await matches.GetByTournamentAsync(tournamentId, ct);
            var existingGroupMatches = already.Where(m => m.Round == 0).ToList();
            if (existingGroupMatches.Count > 0)
                return OperationResult<List<MatchDto>>.Ok(existingGroupMatches);

            var tournamentGroups = await groups.GetByTournamentAsync(tournamentId, ct);
            if (tournamentGroups.Count == 0)
                return OperationResult<List<MatchDto>>.Conflict("Tournament has no groups to generate matches for.");

            var generated = new List<MatchDto>();

            foreach (var group in tournamentGroups)
            {
                var roster = group.Teams;

                // Todos contra todos dentro del grupo.
                for (var i = 0; i < roster.Count; i++)
                {
                    for (var j = i + 1; j < roster.Count; j++)
                    {
                        var match = BuildMatch(tournamentId, group.Id, roster[i].Id, roster[j].Id);
                        generated.Add(await matches.AddAsync(match, ct));
                    }
                }
            }

            if (generated.Count == 0)
                return OperationResult<List<MatchDto>>.Conflict("Each group needs at least two teams.");

            await uow.CommitAsync(ct);
            return OperationResult<List<MatchDto>>.Ok(generated);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OperationResult<List<MatchDto>>> SimulateGroupMatchesAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
            return OperationResult<List<MatchDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        var all = await matches.GetByTournamentAsync(tournamentId, ct);
        var groupMatches = all.Where(m => m.Round == 0).ToList();
        if (groupMatches.Count == 0)
            return OperationResult<List<MatchDto>>.Conflict("Create the group matches first.");

        foreach (var match in groupMatches.Where(m => !m.IsCompleted))
        {
            var homeGoals = Random.Shared.Next(0, 6);
            var awayGoals = Random.Shared.Next(0, 6);
            MatchWinner? winner = homeGoals == awayGoals
                ? null
                : homeGoals > awayGoals ? MatchWinner.HOME : MatchWinner.VISITOR;

            await matches.ReplaceAsync(match with
            {
                Score = new ScoreDto(homeGoals, awayGoals),
                Winner = winner,
                IsCompleted = true
            }, ct);
        }

        await uow.CommitAsync(ct);
        var fresh = await matches.GetByTournamentAsync(tournamentId, ct);
        return OperationResult<List<MatchDto>>.Ok(fresh.Where(m => m.Round == 0).ToList());
    }

    public async Task<OperationResult<List<GroupStandingDto>>> GetStandingsAsync(string tournamentId, CancellationToken ct = default)
    {
        if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
            return OperationResult<List<GroupStandingDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        var tournamentGroups = await groups.GetByTournamentAsync(tournamentId, ct);
        var all = await matches.GetByTournamentAsync(tournamentId, ct);
        return OperationResult<List<GroupStandingDto>>.Ok(BuildStandings(tournamentGroups, all));
    }

    public async Task<OperationResult<ChampionDto>> PlayKnockoutAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var scope = await uow.BeginAsync(ct);

        try
        {
            if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
                return OperationResult<ChampionDto>.NotFound($"Tournament '{tournamentId}' not found.");

            var all = await matches.GetByTournamentAsync(tournamentId, ct);
            var knockout = all.Where(m => m.Round > 0).ToList();
            if (knockout.Count > 0)
                return FinishedChampion(knockout);

            var groupMatches = all.Where(m => m.Round == 0).ToList();
            if (groupMatches.Count == 0)
                return OperationResult<ChampionDto>.Conflict("Create the group matches first.");
            if (groupMatches.Any(m => !m.IsCompleted))
                return OperationResult<ChampionDto>.Conflict("Group matches are still pending.");

            var tournamentGroups = await groups.GetByTournamentAsync(tournamentId, ct);
            var standings = BuildStandings(tournamentGroups, groupMatches);
            var qualifiers = standings
                .SelectMany(group => group.Table.Take(2))
                .OrderByDescending(row => row.Wins)
                .ThenByDescending(row => row.GoalsFor)
                .ThenBy(row => row.GoalsAgainst)
                .ThenBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (qualifiers.Count == 0)
                return OperationResult<ChampionDto>.Conflict("There are no teams to advance.");

            var names = tournamentGroups
                .SelectMany(g => g.Teams)
                .GroupBy(t => t.Id)
                .ToDictionary(g => g.Key, g => g.First().Name);

            var seeds = qualifiers.Select(row => row.TeamId).ToList();
            var rounds = new List<KnockoutRoundDto>();
            var round = 1;

            while (seeds.Count > 1)
            {
                var pairs = TournamentServices.Domain.KnockoutPairing.Pair(seeds);
                var next = new List<string>();
                var roundMatches = new List<MatchDto>();

                foreach (var (homeId, awayId) in pairs)
                {
                    if (awayId is null)
                    {
                        next.Add(homeId);
                        continue;
                    }

                    var (homeGoals, awayGoals) = TournamentServices.Domain.KnockoutScore.Play(Random.Shared);
                    var winner = homeGoals > awayGoals ? MatchWinner.HOME : MatchWinner.VISITOR;
                    var match = new MatchDto(
                        $"mtc-{Guid.NewGuid().ToString("N")[..8]}",
                        tournamentId,
                        null,
                        homeId,
                        awayId,
                        null,
                        null,
                        new ScoreDto(homeGoals, awayGoals),
                        winner,
                        true,
                        round);

                    var saved = await matches.AddAsync(match, ct);
                    roundMatches.Add(saved);
                    next.Add(winner == MatchWinner.HOME ? homeId : awayId);
                }

                if (next.Count >= seeds.Count)
                    return OperationResult<ChampionDto>.Conflict("Knockout pairing did not reduce the field.");

                if (roundMatches.Count > 0)
                    rounds.Add(new KnockoutRoundDto(round, roundMatches));

                seeds = next;
                round++;
            }

            await uow.CommitAsync(ct);
            return OperationResult<ChampionDto>.Ok(new ChampionDto(seeds[0], names[seeds[0]], rounds));
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    private static OperationResult<ChampionDto> FinishedChampion(List<MatchDto> knockout)
    {
        var lastRound = knockout.Max(m => m.Round);
        var final = knockout.Where(m => m.Round == lastRound).ToList();
        if (final.Count != 1 || final[0].Winner is null)
            return OperationResult<ChampionDto>.Conflict("Knockout is incomplete.");

        var match = final[0];
        var winnerId = match.Winner == MatchWinner.HOME ? match.HomeTeamId : match.VisitorTeamId;
        var winnerName = match.Winner == MatchWinner.HOME ? match.HomeTeam?.Name : match.VisitorTeam?.Name;
        var rounds = knockout
            .GroupBy(m => m.Round)
            .OrderBy(g => g.Key)
            .Select(g => new KnockoutRoundDto(g.Key, g.ToList()))
            .ToList();

        return OperationResult<ChampionDto>.Ok(new ChampionDto(winnerId, winnerName ?? winnerId, rounds));
    }

    private static List<GroupStandingDto> BuildStandings(List<GroupDto> tournamentGroups, List<MatchDto> matches)
    {
        var tables = new List<GroupStandingDto>();
        foreach (var group in tournamentGroups.OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase))
        {
            var played = matches
                .Where(m => m.Round == 0 && m.IsCompleted && m.GroupId == group.Id)
                .Select(m => (m.HomeTeamId, m.Score.HomeTeamScore, m.VisitorTeamId, m.Score.VisitorTeamScore))
                .ToList();

            var rows = TournamentServices.Domain.Standings.Rank(group.Teams.Select(t => (t.Id, t.Name)).ToList(), played)
                .Select(row => new StandingRowDto(row.TeamId, row.Name, row.Wins, row.GoalsFor, row.GoalsAgainst))
                .ToList();

            tables.Add(new GroupStandingDto(group.Id, group.Name, rows));
        }

        return tables;
    }

    private static MatchDto BuildMatch(string tournamentId, string? groupId, string homeTeamId, string visitorTeamId) =>
        new(
            $"mtc-{Guid.NewGuid().ToString("N")[..8]}",
            tournamentId,
            groupId,
            homeTeamId,
            visitorTeamId,
            null,
            null,
            new ScoreDto(0, 0),
            null,
            false);
}
