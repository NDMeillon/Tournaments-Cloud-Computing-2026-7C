using TournamentServices.Api.Dtos;
using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public class TournamentDelegate(
    ITournamentRepository tournaments,
    IGroupRepository groups,
    IMatchRepository matches,
    IUnitOfWork uow) : ITournamentDelegate
{
    public Task<List<TournamentDto>> GetAllAsync(CancellationToken ct = default) =>
        tournaments.GetAllAsync(ct);

    public async Task<OperationResult<TournamentDto>> GetByIdAsync(string tournamentId, CancellationToken ct = default)
    {
        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<TournamentDto>.NotFound($"Tournament '{tournamentId}' not found.");

        // Hidrata grupos y partidos, que viven en sus propios almacenes.
        var hydrated = tournament with
        {
            Groups = await groups.GetByTournamentAsync(tournamentId, ct),
            Matches = await matches.GetByTournamentAsync(tournamentId, ct)
        };

        return OperationResult<TournamentDto>.Ok(hydrated);
    }

    public async Task<OperationResult<TournamentDto>> CreateAsync(CreateTournamentDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = new TournamentDto(
            $"trn-{Guid.NewGuid().ToString("N")[..8]}",
            dto.Name,
            dto.Format,
            [],
            []);

        await tournaments.AddAsync(tournament, ct);
        await uow.CommitAsync(ct);

        return OperationResult<TournamentDto>.Ok(tournament);
    }

    public async Task<OperationResult<TournamentDto>> CreateWithGroupsAsync(CreateTournamentDto dto, CancellationToken ct = default)
    {
        await using var scope = await uow.BeginAsync(ct);

        try
        {
            var tournament = new TournamentDto(
                $"trn-{Guid.NewGuid().ToString("N")[..8]}",
                dto.Name,
                dto.Format,
                [],
                []);

            await tournaments.AddAsync(tournament, ct);

            var created = new List<GroupDto>(dto.Format.NumberOfGroups);
            for (var i = 0; i < dto.Format.NumberOfGroups; i++)
            {
                var group = new GroupDto(
                    $"grp-{Guid.NewGuid().ToString("N")[..8]}",
                    $"Grupo {(char)('A' + i)}",
                    tournament.Id,
                    []);

                await groups.AddAsync(group, ct);
                created.Add(group);
            }

            await uow.CommitAsync(ct);
            return OperationResult<TournamentDto>.Ok(tournament with { Groups = created });
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }

    public async Task<OperationResult<TournamentDto>> UpdateAsync(string tournamentId, UpdateTournamentDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<TournamentDto>.NotFound($"Tournament '{tournamentId}' not found.");

        var existingGroups = await groups.GetByTournamentAsync(tournamentId, ct);
        if (dto.Format.NumberOfGroups < existingGroups.Count)
            return OperationResult<TournamentDto>.Conflict(
                $"Cannot shrink to {dto.Format.NumberOfGroups} groups: tournament already has {existingGroups.Count}.");

        var updated = tournament with { Name = dto.Name, Format = dto.Format };
        await tournaments.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<TournamentDto>.Ok(updated);
    }

    public async Task<OperationResult<TournamentDto>> PatchAsync(string tournamentId, PatchTournamentDto dto, CancellationToken ct = default)
    {
        await using var _ = await uow.BeginAsync(ct);

        var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
        if (tournament is null)
            return OperationResult<TournamentDto>.NotFound($"Tournament '{tournamentId}' not found.");

        // Solo se sobrescribe lo que viene informado.
        var format = tournament.Format;
        if (dto.Format is not null)
        {
            format = new TournamentFormatDto(
                dto.Format.MaxTeamsPerGroup ?? format.MaxTeamsPerGroup,
                dto.Format.NumberOfGroups ?? format.NumberOfGroups,
                dto.Format.Type ?? format.Type);
        }

        var updated = tournament with
        {
            Name = dto.Name ?? tournament.Name,
            Format = format
        };

        await tournaments.ReplaceAsync(updated, ct);
        await uow.CommitAsync(ct);

        return OperationResult<TournamentDto>.Ok(updated);
    }

    public async Task<OperationResult<bool>> DeleteAsync(string tournamentId, CancellationToken ct = default)
    {
        await using var scope = await uow.BeginAsync(ct);

        try
        {
            var tournament = await tournaments.GetByIdAsync(tournamentId, ct);
            if (tournament is null)
                return OperationResult<bool>.NotFound($"Tournament '{tournamentId}' not found.");

            // Cascada: partidos y grupos se van con el torneo. Los equipos NO.
            await matches.RemoveByTournamentAsync(tournamentId, ct);
            await groups.RemoveByTournamentAsync(tournamentId, ct);
            await tournaments.RemoveAsync(tournamentId, ct);

            await uow.CommitAsync(ct);
            return OperationResult<bool>.Ok(true);
        }
        catch
        {
            await uow.RollbackAsync(ct);
            throw;
        }
    }
}
