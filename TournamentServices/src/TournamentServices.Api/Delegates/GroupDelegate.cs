using TournamentServices.Api.Dtos;
using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public class GroupDelegate(
    IGroupRepository groups,
    ITournamentRepository tournaments,
    ITeamRepository teams,
    IUnitOfWork uow) : IGroupDelegate
{
    public async Task<OperationResult<List<GroupDto>>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        if (await tournaments.GetByIdAsync(tournamentId, ct) is null)
            return OperationResult<List<GroupDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        return OperationResult<List<GroupDto>>.Ok(await groups.GetByTournamentAsync(tournamentId, ct));
    }

    public async Task<OperationResult<GroupDto>> GetByIdAsync(string tournamentId, string groupId, CancellationToken ct = default)
    {
        var group = await groups.GetByIdAsync(groupId, ct);
        if (group is null || group.TournamentId != tournamentId)
            return OperationResult<GroupDto>.NotFound($"Group '{groupId}' not found in tournament '{tournamentId}'.");

        return OperationResult<GroupDto>.Ok(group);
    }

    public async Task<OperationResult<GroupDto>> CreateAsync(string tournamentId, CreateGroupDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<GroupDto>.NotFound($"Tournament '{tournamentId}' not found.");

        var existing = await groups.GetByTournamentAsync(tournamentId, ct);

        if (existing.Count >= tournament.Format.NumberOfGroups)
            return OperationResult<GroupDto>.Conflict(
                $"Tournament '{tournamentId}' already has its {tournament.Format.NumberOfGroups} groups.");

        if (existing.Any(g => string.Equals(g.Name, dto.Name, StringComparison.OrdinalIgnoreCase)))
            return OperationResult<GroupDto>.Conflict($"A group named '{dto.Name}' already exists in this tournament.");

        var group = new GroupDto($"grp-{Guid.NewGuid().ToString("N")[..8]}", dto.Name, tournamentId, []);
        await groups.AddAsync(group, ct);
        await uow.CommitAsync(ct);

        return OperationResult<GroupDto>.Ok(group);
    }

    public async Task<OperationResult<GroupDto>> UpdateAsync(string tournamentId, string groupId, UpdateGroupDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var group = await groups.GetByIdAsync(groupId, ct);
        if (group is null || group.TournamentId != tournamentId)
            return OperationResult<GroupDto>.NotFound($"Group '{groupId}' not found in tournament '{tournamentId}'.");

        var siblings = await groups.GetByTournamentAsync(tournamentId, ct);
        if (siblings.Any(g => g.Id != groupId && string.Equals(g.Name, dto.Name, StringComparison.OrdinalIgnoreCase)))
            return OperationResult<GroupDto>.Conflict($"A group named '{dto.Name}' already exists in this tournament.");

        var updated = group with { Name = dto.Name };
        await groups.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<GroupDto>.Ok(updated);
    }

    public async Task<OperationResult<bool>> DeleteAsync(string tournamentId, string groupId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var group = await groups.GetByIdAsync(groupId, ct);
        if (group is null || group.TournamentId != tournamentId)
            return OperationResult<bool>.NotFound($"Group '{groupId}' not found in tournament '{tournamentId}'.");

        await groups.RemoveAsync(groupId, ct);
        await uow.CommitAsync(ct);

        return OperationResult<bool>.Ok(true);
    }

    public async Task<OperationResult<GroupDto>> AssignTeamsAsync(string tournamentId, string groupId, AssignTeamsDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<GroupDto>.NotFound($"Tournament '{tournamentId}' not found.");

        var group = await groups.GetByIdAsync(groupId, ct);
        if (group is null || group.TournamentId != tournamentId)
            return OperationResult<GroupDto>.NotFound($"Group '{groupId}' not found in tournament '{tournamentId}'.");

        // 1. Todos los equipos deben existir.
        var resolved = new List<TeamDto>(dto.TeamIds.Count);
        foreach (var teamId in dto.TeamIds)
        {
            var team = await teams.GetByIdAsync(teamId, ct);
            if (team is null)
                return OperationResult<GroupDto>.NotFound($"Team '{teamId}' not found.");
            resolved.Add(team);
        }

        // 2. No exceder el cupo del formato.
        if (resolved.Count > tournament.Format.MaxTeamsPerGroup)
            return OperationResult<GroupDto>.Conflict(
                $"Group '{groupId}' admits at most {tournament.Format.MaxTeamsPerGroup} teams, got {resolved.Count}.");

        // 3. Un equipo no puede estar en dos grupos del mismo torneo.
        var siblings = await groups.GetByTournamentAsync(tournamentId, ct);
        var takenElsewhere = siblings
            .Where(g => g.Id != groupId)
            .SelectMany(g => g.Teams.Select(t => t.Id))
            .ToHashSet();

        var clash = resolved.FirstOrDefault(t => takenElsewhere.Contains(t.Id));
        if (clash is not null)
            return OperationResult<GroupDto>.Conflict(
                $"Team '{clash.Name}' is already assigned to another group in this tournament.");

        var updated = group with { Teams = resolved };
        await groups.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<GroupDto>.Ok(updated);
    }
}
