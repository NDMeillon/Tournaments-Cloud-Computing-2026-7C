using System.Collections.Concurrent;
using TournamentServices.Api.Dtos;

namespace TournamentServices.Api.Repositories;

/// <summary>
/// Almacen compartido en memoria. Registrado como Singleton.
/// Reemplazar por DbContext cuando entre la persistencia real.
/// </summary>
public class InMemoryStore
{
    public ConcurrentDictionary<string, TournamentDto> Tournaments { get; } = new();
    public ConcurrentDictionary<string, TeamDto> Teams { get; } = new();
    public ConcurrentDictionary<string, GroupDto> Groups { get; } = new();
    public ConcurrentDictionary<string, MatchDto> Matches { get; } = new();

    public readonly SemaphoreSlim Gate = new(1, 1);

    public void Reset()
    {
        Tournaments.Clear();
        Teams.Clear();
        Groups.Clear();
        Matches.Clear();
    }
}

public class InMemoryUnitOfWork(InMemoryStore store) : IUnitOfWork
{
    private sealed class Scope(SemaphoreSlim gate) : IAsyncDisposable
    {
        public ValueTask DisposeAsync()
        {
            gate.Release();
            return ValueTask.CompletedTask;
        }
    }

    public async Task<IAsyncDisposable> BeginAsync(CancellationToken ct = default)
    {
        await store.Gate.WaitAsync(ct);
        return new Scope(store.Gate);
    }

    // El store in-memory aplica los cambios de inmediato: no hay commit diferido.
    public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;

    public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;
}

public class InMemoryTournamentRepository(InMemoryStore store) : ITournamentRepository
{
    public Task<List<TournamentDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(store.Tournaments.Values.ToList());

    public Task<TournamentDto?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(store.Tournaments.TryGetValue(id, out var t) ? t : null);

    public Task<TournamentDto> AddAsync(TournamentDto tournament, CancellationToken ct = default)
    {
        store.Tournaments[tournament.Id] = tournament;
        return Task.FromResult(tournament);
    }

    public Task<TournamentDto?> ReplaceAsync(TournamentDto tournament, CancellationToken ct = default)
    {
        if (!store.Tournaments.ContainsKey(tournament.Id)) return Task.FromResult<TournamentDto?>(null);
        store.Tournaments[tournament.Id] = tournament;
        return Task.FromResult<TournamentDto?>(tournament);
    }

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(store.Tournaments.TryRemove(id, out _));
}

public class InMemoryTeamRepository(InMemoryStore store) : ITeamRepository
{
    public Task<List<TeamDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(store.Teams.Values.ToList());

    public Task<TeamDto?> GetByIdAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(store.Teams.TryGetValue(id, out var t) ? t : null);

    public Task<TeamDto?> GetByNameAsync(string name, CancellationToken ct = default) =>
        Task.FromResult(store.Teams.Values
            .FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task<TeamDto> AddAsync(TeamDto team, CancellationToken ct = default)
    {
        store.Teams[team.Id] = team;
        return Task.FromResult(team);
    }

    public Task<TeamDto?> ReplaceAsync(TeamDto team, CancellationToken ct = default)
    {
        if (!store.Teams.ContainsKey(team.Id)) return Task.FromResult<TeamDto?>(null);
        store.Teams[team.Id] = team;
        return Task.FromResult<TeamDto?>(team);
    }

    public Task<bool> RemoveAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(store.Teams.TryRemove(id, out _));
}

public class InMemoryGroupRepository(InMemoryStore store) : IGroupRepository
{
    public Task<List<GroupDto>> GetAllAsync(CancellationToken ct = default) =>
        Task.FromResult(store.Groups.Values.ToList());

    public Task<List<GroupDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default) =>
        Task.FromResult(store.Groups.Values.Where(g => g.TournamentId == tournamentId).ToList());

    public Task<GroupDto?> GetByIdAsync(string groupId, CancellationToken ct = default) =>
        Task.FromResult(store.Groups.TryGetValue(groupId, out var g) ? g : null);

    public Task<GroupDto> AddAsync(GroupDto group, CancellationToken ct = default)
    {
        store.Groups[group.Id] = group;
        return Task.FromResult(group);
    }

    public Task<GroupDto?> ReplaceAsync(GroupDto group, CancellationToken ct = default)
    {
        if (!store.Groups.ContainsKey(group.Id)) return Task.FromResult<GroupDto?>(null);
        store.Groups[group.Id] = group;
        return Task.FromResult<GroupDto?>(group);
    }

    public Task<bool> RemoveAsync(string groupId, CancellationToken ct = default) =>
        Task.FromResult(store.Groups.TryRemove(groupId, out _));

    public Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var ids = store.Groups.Values.Where(g => g.TournamentId == tournamentId).Select(g => g.Id).ToList();
        foreach (var id in ids) store.Groups.TryRemove(id, out _);
        return Task.FromResult(ids.Count);
    }
}

public class InMemoryMatchRepository(InMemoryStore store) : IMatchRepository
{
    public Task<List<MatchDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default) =>
        Task.FromResult(store.Matches.Values.Where(m => m.TournamentId == tournamentId).ToList());

    public Task<MatchDto?> GetByIdAsync(string matchId, CancellationToken ct = default) =>
        Task.FromResult(store.Matches.TryGetValue(matchId, out var m) ? m : null);

    public Task<MatchDto> AddAsync(MatchDto match, CancellationToken ct = default)
    {
        store.Matches[match.Id] = match;
        return Task.FromResult(match);
    }

    public Task<MatchDto?> ReplaceAsync(MatchDto match, CancellationToken ct = default)
    {
        if (!store.Matches.ContainsKey(match.Id)) return Task.FromResult<MatchDto?>(null);
        store.Matches[match.Id] = match;
        return Task.FromResult<MatchDto?>(match);
    }

    public Task<bool> RemoveAsync(string matchId, CancellationToken ct = default) =>
        Task.FromResult(store.Matches.TryRemove(matchId, out _));

    public Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var ids = store.Matches.Values.Where(m => m.TournamentId == tournamentId).Select(m => m.Id).ToList();
        foreach (var id in ids) store.Matches.TryRemove(id, out _);
        return Task.FromResult(ids.Count);
    }
}
