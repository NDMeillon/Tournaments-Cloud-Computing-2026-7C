using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Delegates;

public interface ITeamDelegate
{
    Task<List<TeamDto>> GetAllAsync(CancellationToken ct = default);

    Task<OperationResult<TeamDto>> GetByIdAsync(string teamId, CancellationToken ct = default);

    /// <summary>
    /// Crea el equipo. Devuelve Conflict si ya existe otro con el mismo nombre
    /// (comparacion case-insensitive).
    /// </summary>
    Task<OperationResult<TeamDto>> CreateAsync(CreateTeamDto dto, CancellationToken ct = default);

    Task<OperationResult<TeamDto>> UpdateAsync(string teamId, UpdateTeamDto dto, CancellationToken ct = default);

    /// <summary>
    /// Borra el equipo y lo desasigna de cualquier grupo que lo contenga.
    /// </summary>
    Task<OperationResult<bool>> DeleteAsync(string teamId, CancellationToken ct = default);

    Task<bool> ExistsAsync(string teamId, CancellationToken ct = default);
}
