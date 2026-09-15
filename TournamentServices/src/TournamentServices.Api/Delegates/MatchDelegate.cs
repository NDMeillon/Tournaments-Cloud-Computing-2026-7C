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

        if (await teams.GetByIdAsync(dto.HomeTeamId, ct) is null)
            return OperationResult<MatchDto>.NotFound($"Team '{dto.HomeTeamId}' not found.");

        if (await teams.GetByIdAsync(dto.VisitorTeamId, ct) is null)
            return OperationResult<MatchDto>.NotFound($"Team '{dto.VisitorTeamId}' not found.");

        // Si el partido es de grupo, ambos equipos deben pertenecer a ese grupo.
        if (!string.IsNullOrWhiteSpace(dto.GroupId))
        {
            var group = await groups.GetByIdAsync(dto.GroupId!, ct);
            if (group is null || group.TournamentId != tournamentId)
                return OperationResult<MatchDto>.NotFound($"Group '{dto.GroupId}' not found in tournament '{tournamentId}'.");

            var memberIds = group.Teams.Select(t => t.Id).ToHashSet();
            if (!memberIds.Contains(dto.HomeTeamId) || !memberIds.Contains(dto.VisitorTeamId))
                return OperationResult<MatchDto>.Conflict(
                    $"Both teams must belong to group '{dto.GroupId}'.");
        }

        var match = BuildMatch(tournamentId, dto.GroupId, dto.HomeTeamId, dto.VisitorTeamId);
        await matches.AddAsync(match, ct);
        await uow.CommitAsync(ct);

        return OperationResult<MatchDto>.Ok(match);
    }

    public async Task<OperationResult<MatchDto>> UpdateScoreAsync(string tournamentId, string matchId, UpdateScoreDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var match = await matches.GetByIdAsync(matchId, ct);
        if (match is null || match.TournamentId != tournamentId)
            return OperationResult<MatchDto>.NotFound($"Match '{matchId}' not found in tournament '{tournamentId}'.");

        if (match.IsCompleted)
            return OperationResult<MatchDto>.Conflict($"Match '{matchId}' is already finished; its score is locked.");

        var updated = match with { Score = new ScoreDto(dto.HomeTeamScore, dto.VisitorTeamScore) };
        await matches.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<MatchDto>.Ok(updated);
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
                        await matches.AddAsync(match, ct);
                        generated.Add(match);
                    }
                }
            }

            await uow.CommitAsync(ct);
            return OperationResult<List<MatchDto>>.Ok(generated);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
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
