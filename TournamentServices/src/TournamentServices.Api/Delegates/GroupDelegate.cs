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
                return OperationResult<GroupDto>.Conflict($"Team '{teamId}' is not part of this tournament.");
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

    public async Task<OperationResult<List<GroupDto>>> GenerateAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<List<GroupDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        var existing = await groups.GetByTournamentAsync(tournamentId, ct);
        var usedNames = existing.Select(g => g.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var letter = 0;

        while (existing.Count < tournament.Format.NumberOfGroups)
        {
            string name;
            do
            {
                name = GroupName(letter);
                letter++;
            }
            while (usedNames.Contains(name));

            var group = new GroupDto($"grp-{Guid.NewGuid().ToString("N")[..8]}", name, tournamentId, []);
            await groups.AddAsync(group, ct);
            existing.Add(group);
            usedNames.Add(name);
        }

        await uow.CommitAsync(ct);
        return OperationResult<List<GroupDto>>.Ok(existing);
    }

    public async Task<OperationResult<List<GroupDto>>> FillAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<List<GroupDto>>.NotFound($"Tournament '{tournamentId}' not found.");

        var existing = await groups.GetByTournamentAsync(tournamentId, ct);
        if (existing.Count == 0)
            return OperationResult<List<GroupDto>>.Conflict("Tournament has no groups to fill.");

        var used = existing.SelectMany(g => g.Teams).Select(t => t.Id).ToHashSet();
        var index = 0;
        var filled = new List<GroupDto>(existing.Count);

        foreach (var group in existing)
        {
            var members = group.Teams.ToList();
            while (members.Count < tournament.Format.MaxTeamsPerGroup)
            {
                var taken = await TakeTeamAsync(used, index, ct);
                index = taken.Next;
                used.Add(taken.Team.Id);
                members.Add(taken.Team);
            }

            if (members.Count == group.Teams.Count)
            {
                filled.Add(group);
                continue;
            }

            var saved = await groups.ReplaceAsync(group with { Teams = members }, ct);
            filled.Add(saved!);
        }

        await uow.CommitAsync(ct);
        return OperationResult<List<GroupDto>>.Ok(filled);
    }

    private async Task<(TeamDto Team, int Next)> TakeTeamAsync(HashSet<string> used, int index, CancellationToken ct)
    {
        while (true)
        {
            var name = index < Squad.Length ? Squad[index] : $"Equipo {index + 1}";
            index++;
            var existing = await teams.GetByNameAsync(name, ct);
            if (existing is null)
            {
                var created = new TeamDto($"team-{Guid.NewGuid().ToString("N")[..8]}", name);
                await teams.AddAsync(created, ct);
                return (created, index);
            }

            if (!used.Contains(existing.Id))
                return (existing, index);
        }
    }

    private static string GroupName(int index) =>
        index < 26 ? $"Grupo {(char)('A' + index)}" : $"Grupo {index + 1}";

    private static readonly string[] Squad =
    [
        "Mexico", "Polonia", "Arabia Saudita", "Argentina",
        "Canada", "Belgica", "Marruecos", "Croacia",
        "Estados Unidos", "Gales", "Inglaterra", "Iran",
        "Francia", "Australia", "Dinamarca", "Tunez",
        "Espana", "Costa Rica", "Alemania", "Japon",
        "Brasil", "Serbia", "Suiza", "Camerun",
        "Portugal", "Ghana", "Uruguay", "Corea del Sur",
        "Paises Bajos", "Senegal", "Ecuador", "Qatar"
    ];
}
