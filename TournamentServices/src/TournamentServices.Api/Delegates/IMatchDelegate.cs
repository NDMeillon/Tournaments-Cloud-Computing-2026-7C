using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Delegates;

public interface IMatchDelegate
{
    Task<OperationResult<List<MatchDto>>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default);

    Task<OperationResult<MatchDto>> GetByIdAsync(string tournamentId, string matchId, CancellationToken ct = default);

    /// <summary>
    /// Crea un partido. Reglas:
    /// - ambos equipos deben existir y ser distintos
    /// - si se indica GroupId, ambos equipos deben pertenecer a ese grupo
    /// </summary>
    Task<OperationResult<MatchDto>> CreateAsync(string tournamentId, CreateMatchDto dto, CancellationToken ct = default);

    /// <summary>
    /// Actualiza el marcador. Devuelve Conflict si el partido ya esta finalizado.
    /// </summary>
    Task<OperationResult<MatchDto>> UpdateScoreAsync(string tournamentId, string matchId, UpdateScoreDto dto, CancellationToken ct = default);

    Task<OperationResult<bool>> DeleteAsync(string tournamentId, string matchId, CancellationToken ct = default);

    /// <summary>
    /// Genera el round-robin de todos los grupos del torneo en una sola transaccion.
    /// </summary>
    Task<OperationResult<List<MatchDto>>> GenerateRoundRobinAsync(string tournamentId, CancellationToken ct = default);
}
