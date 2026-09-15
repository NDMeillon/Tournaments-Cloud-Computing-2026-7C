using TournamentServices.Api.Dtos;
using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public class TeamDelegate(
    ITeamRepository teams,
    IGroupRepository groups,
    IUnitOfWork uow) : ITeamDelegate
{
    public Task<List<TeamDto>> GetAllAsync(CancellationToken ct = default) =>
        teams.GetAllAsync(ct);

    public async Task<OperationResult<TeamDto>> GetByIdAsync(string teamId, CancellationToken ct = default)
    {
        var team = await teams.GetByIdAsync(teamId, ct);
        return team is null
            ? OperationResult<TeamDto>.NotFound($"Team '{teamId}' not found.")
            : OperationResult<TeamDto>.Ok(team);
    }

    public async Task<OperationResult<TeamDto>> CreateAsync(CreateTeamDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var existing = await teams.GetByNameAsync(dto.Name, ct);
        if (existing is not null)
            return OperationResult<TeamDto>.Conflict($"A team named '{dto.Name}' already exists.");

        var team = new TeamDto($"team-{Guid.NewGuid().ToString("N")[..8]}", dto.Name);
        await teams.AddAsync(team, ct);
        await uow.CommitAsync(ct);

        return OperationResult<TeamDto>.Ok(team);
    }

    public async Task<OperationResult<TeamDto>> UpdateAsync(string teamId, UpdateTeamDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var team = await teams.GetByIdAsync(teamId, ct);
        if (team is null)
            return OperationResult<TeamDto>.NotFound($"Team '{teamId}' not found.");

        var clash = await teams.GetByNameAsync(dto.Name, ct);
        if (clash is not null && clash.Id != teamId)
            return OperationResult<TeamDto>.Conflict($"A team named '{dto.Name}' already exists.");

        var updated = team with { Name = dto.Name };
        await teams.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<TeamDto>.Ok(updated);
    }

    public async Task<OperationResult<bool>> DeleteAsync(string teamId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var team = await teams.GetByIdAsync(teamId, ct);
        if (team is null)
            return OperationResult<bool>.NotFound($"Team '{teamId}' not found.");

        // Desasigna el equipo de cualquier grupo antes de borrarlo,
        // para no dejar referencias colgantes.
        var affected = await groups.GetAllAsync(ct);
        foreach (var group in affected.Where(g => g.Teams.Any(t => t.Id == teamId)))
        {
            var trimmed = group with { Teams = group.Teams.Where(t => t.Id != teamId).ToList() };
            await groups.ReplaceAsync(trimmed, ct);
        }

        await teams.RemoveAsync(teamId, ct);
        await uow.CommitAsync(ct);

        return OperationResult<bool>.Ok(true);
    }

    public async Task<bool> ExistsAsync(string teamId, CancellationToken ct = default) =>
        await teams.GetByIdAsync(teamId, ct) is not null;
}
