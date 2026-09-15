using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Repositories;

public interface ITournamentRepository
{
    Task<List<TournamentDto>> GetAllAsync(CancellationToken ct = default);
    Task<TournamentDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TournamentDto> AddAsync(TournamentDto tournament, CancellationToken ct = default);
    Task<TournamentDto?> ReplaceAsync(TournamentDto tournament, CancellationToken ct = default);
    Task<bool> RemoveAsync(string id, CancellationToken ct = default);
}

public interface ITeamRepository
{
    Task<List<TeamDto>> GetAllAsync(CancellationToken ct = default);
    Task<TeamDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<TeamDto?> GetByNameAsync(string name, CancellationToken ct = default);
    Task<TeamDto> AddAsync(TeamDto team, CancellationToken ct = default);
    Task<TeamDto?> ReplaceAsync(TeamDto team, CancellationToken ct = default);
    Task<bool> RemoveAsync(string id, CancellationToken ct = default);
}

public interface IGroupRepository
{
    Task<List<GroupDto>> GetAllAsync(CancellationToken ct = default);
    Task<List<GroupDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default);
    Task<GroupDto?> GetByIdAsync(string groupId, CancellationToken ct = default);
    Task<GroupDto> AddAsync(GroupDto group, CancellationToken ct = default);
    Task<GroupDto?> ReplaceAsync(GroupDto group, CancellationToken ct = default);
    Task<bool> RemoveAsync(string groupId, CancellationToken ct = default);
    Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default);
}

public interface IMatchRepository
{
    Task<List<MatchDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default);
    Task<MatchDto?> GetByIdAsync(string matchId, CancellationToken ct = default);
    Task<MatchDto> AddAsync(MatchDto match, CancellationToken ct = default);
    Task<MatchDto?> ReplaceAsync(MatchDto match, CancellationToken ct = default);
    Task<bool> RemoveAsync(string matchId, CancellationToken ct = default);
    Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default);
}

/// <summary>
/// Punto unico para agrupar operaciones multi-paso. Con el store in-memory
/// se implementa como un lock global; al migrar a EF Core se reemplaza por
/// Database.BeginTransactionAsync sin tocar los delegados.
/// </summary>
public interface IUnitOfWork
{
    Task<IAsyncDisposable> BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
