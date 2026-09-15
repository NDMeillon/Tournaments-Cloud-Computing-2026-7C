using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Delegates;

public interface ITournamentDelegate
{
    Task<List<TournamentDto>> GetAllAsync(CancellationToken ct = default);

    Task<OperationResult<TournamentDto>> GetByIdAsync(string tournamentId, CancellationToken ct = default);

    Task<OperationResult<TournamentDto>> CreateAsync(CreateTournamentDto dto, CancellationToken ct = default);

    /// <summary>
    /// Crea el torneo y, en la misma transaccion, sus grupos vacios
    /// segun Format.NumberOfGroups (Grupo A, Grupo B, ...).
    /// </summary>
    Task<OperationResult<TournamentDto>> CreateWithGroupsAsync(CreateTournamentDto dto, CancellationToken ct = default);

    Task<OperationResult<TournamentDto>> UpdateAsync(string tournamentId, UpdateTournamentDto dto, CancellationToken ct = default);

    Task<OperationResult<TournamentDto>> PatchAsync(string tournamentId, PatchTournamentDto dto, CancellationToken ct = default);

    /// <summary>
    /// Borra el torneo junto con sus grupos y partidos. No borra equipos.
    /// </summary>
    Task<OperationResult<bool>> DeleteAsync(string tournamentId, CancellationToken ct = default);
}
