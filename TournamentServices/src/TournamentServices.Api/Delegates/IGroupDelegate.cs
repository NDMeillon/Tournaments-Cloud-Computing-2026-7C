using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Delegates;

public interface IGroupDelegate
{
    Task<OperationResult<List<GroupDto>>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default);

    Task<OperationResult<GroupDto>> GetByIdAsync(string tournamentId, string groupId, CancellationToken ct = default);

    /// <summary>
    /// Crea un grupo. Devuelve Conflict si el torneo ya alcanzo
    /// Format.NumberOfGroups o si el nombre ya existe en ese torneo.
    /// </summary>
    Task<OperationResult<GroupDto>> CreateAsync(string tournamentId, CreateGroupDto dto, CancellationToken ct = default);

    Task<OperationResult<GroupDto>> UpdateAsync(string tournamentId, string groupId, UpdateGroupDto dto, CancellationToken ct = default);

    Task<OperationResult<bool>> DeleteAsync(string tournamentId, string groupId, CancellationToken ct = default);

    /// <summary>
    /// Asigna equipos al grupo. Reglas:
    /// - todos los equipos deben existir
    /// - no se puede exceder Format.MaxTeamsPerGroup
    /// - un equipo no puede estar en dos grupos del mismo torneo
    /// </summary>
    Task<OperationResult<GroupDto>> AssignTeamsAsync(string tournamentId, string groupId, AssignTeamsDto dto, CancellationToken ct = default);
}
