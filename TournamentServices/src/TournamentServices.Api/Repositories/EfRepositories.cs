using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using TournamentServices.Api.Dtos;
using TournamentServices.Infrastructure;
using DomainMatch = TournamentServices.Domain.Match;
using DomainGroup = TournamentServices.Domain.Group;
using DomainTeam = TournamentServices.Domain.Team;
using DomainTournament = TournamentServices.Domain.Tournament;
using DomainType = TournamentServices.Domain.TournamentType;

namespace TournamentServices.Api.Repositories;

public class EfUnitOfWork(TournamentDbContext db) : IUnitOfWork
{
    private IDbContextTransaction? _tx;
    private bool _completed;

    public async Task<IAsyncDisposable> BeginAsync(CancellationToken ct = default)
    {
        if (db.Database.IsRelational() && _tx is null)
            _tx = await db.Database.BeginTransactionAsync(ct);

        return new Scope(this);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (_tx is not null)
        {
            await _tx.CommitAsync(ct);
            await _tx.DisposeAsync();
            _tx = null;
        }

        _completed = true;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (_tx is not null)
        {
            await _tx.RollbackAsync(ct);
            await _tx.DisposeAsync();
            _tx = null;
        }

        db.ChangeTracker.Clear();
        _completed = true;
    }

    private sealed class Scope(EfUnitOfWork owner) : IAsyncDisposable
    {
        public async ValueTask DisposeAsync()
        {
            if (!owner._completed)
                await owner.RollbackAsync();
        }
    }
}

public class EfTeamRepository(TournamentDbContext db) : ITeamRepository
{
    public async Task<List<TeamDto>> GetAllAsync(CancellationToken ct = default) =>
        await db.Teams.AsNoTracking().OrderBy(t => t.Name).Select(t => new TeamDto(t.Id, t.Name)).ToListAsync(ct);

    public async Task<TeamDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var team = await db.Teams.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return team is null ? null : new TeamDto(team.Id, team.Name);
    }

    public async Task<TeamDto?> GetByNameAsync(string name, CancellationToken ct = default)
    {
        var team = await db.Teams.AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower(), ct);
        return team is null ? null : new TeamDto(team.Id, team.Name);
    }

    public async Task<TeamDto> AddAsync(TeamDto team, CancellationToken ct = default)
    {
        db.Teams.Add(new DomainTeam { Id = team.Id, Name = team.Name });
        await db.SaveChangesAsync(ct);
        return team;
    }

    public async Task<TeamDto?> ReplaceAsync(TeamDto team, CancellationToken ct = default)
    {
        var entity = await db.Teams.FirstOrDefaultAsync(t => t.Id == team.Id, ct);
        if (entity is null) return null;
        entity.Name = team.Name;
        await db.SaveChangesAsync(ct);
        return team;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var entity = await db.Teams.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return false;
        db.Teams.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class EfTournamentRepository(TournamentDbContext db) : ITournamentRepository
{
    public async Task<List<TournamentDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Tournaments.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
        return rows.Select(t => EntityMap.ToDto(t)).ToList();
    }

    public async Task<TournamentDto?> GetByIdAsync(string id, CancellationToken ct = default)
    {
        var row = await db.Tournaments.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
        return row is null ? null : EntityMap.ToDto(row);
    }

    public async Task<TournamentDto> AddAsync(TournamentDto tournament, CancellationToken ct = default)
    {
        db.Tournaments.Add(EntityMap.ToEntity(tournament));
        await db.SaveChangesAsync(ct);
        return tournament;
    }

    public async Task<TournamentDto?> ReplaceAsync(TournamentDto tournament, CancellationToken ct = default)
    {
        var entity = await db.Tournaments.FirstOrDefaultAsync(t => t.Id == tournament.Id, ct);
        if (entity is null) return null;
        entity.Name = tournament.Name;
        entity.MaxTeamsPerGroup = tournament.Format.MaxTeamsPerGroup;
        entity.NumberOfGroups = tournament.Format.NumberOfGroups;
        entity.Type = EntityMap.ToDomain(tournament.Format.Type);
        await db.SaveChangesAsync(ct);
        return tournament;
    }

    public async Task<bool> RemoveAsync(string id, CancellationToken ct = default)
    {
        var entity = await db.Tournaments.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (entity is null) return false;
        db.Tournaments.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }
}

public class EfGroupRepository(TournamentDbContext db) : IGroupRepository
{
    public async Task<List<GroupDto>> GetAllAsync(CancellationToken ct = default)
    {
        var rows = await db.Groups.AsNoTracking().Include(g => g.Teams).ToListAsync(ct);
        return rows.Select(EntityMap.ToDto).ToList();
    }

    public async Task<List<GroupDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var rows = await db.Groups.AsNoTracking()
            .Include(g => g.Teams)
            .Where(g => g.TournamentId == tournamentId)
            .OrderBy(g => g.Name)
            .ToListAsync(ct);
        return rows.Select(EntityMap.ToDto).ToList();
    }

    public async Task<GroupDto?> GetByIdAsync(string groupId, CancellationToken ct = default)
    {
        var row = await db.Groups.AsNoTracking().Include(g => g.Teams).FirstOrDefaultAsync(g => g.Id == groupId, ct);
        return row is null ? null : EntityMap.ToDto(row);
    }

    public async Task<GroupDto> AddAsync(GroupDto group, CancellationToken ct = default)
    {
        db.Groups.Add(new DomainGroup
        {
            Id = group.Id,
            Name = group.Name,
            TournamentId = group.TournamentId
        });
        await db.SaveChangesAsync(ct);
        return group;
    }

    public async Task<GroupDto?> ReplaceAsync(GroupDto group, CancellationToken ct = default)
    {
        var entity = await db.Groups.Include(g => g.Teams).FirstOrDefaultAsync(g => g.Id == group.Id, ct);
        if (entity is null) return null;

        entity.Name = group.Name;
        var ids = group.Teams.Select(t => t.Id).ToList();
        var teams = await db.Teams.Where(t => ids.Contains(t.Id)).ToListAsync(ct);

        entity.Teams.Clear();
        foreach (var id in ids)
        {
            var team = teams.FirstOrDefault(t => t.Id == id);
            if (team is not null)
                entity.Teams.Add(team);
        }

        await db.SaveChangesAsync(ct);
        return EntityMap.ToDto(entity);
    }

    public async Task<bool> RemoveAsync(string groupId, CancellationToken ct = default)
    {
        var entity = await db.Groups.FirstOrDefaultAsync(g => g.Id == groupId, ct);
        if (entity is null) return false;
        db.Groups.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var rows = await db.Groups.Where(g => g.TournamentId == tournamentId).ToListAsync(ct);
        db.Groups.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }
}

public class EfMatchRepository(TournamentDbContext db) : IMatchRepository
{
    private IQueryable<DomainMatch> Detailed =>
        db.Matches.AsNoTracking().Include(m => m.HomeTeam).Include(m => m.VisitorTeam);

    public async Task<List<MatchDto>> GetByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var rows = await Detailed.Where(m => m.TournamentId == tournamentId).ToListAsync(ct);
        return rows.Select(EntityMap.ToDto).ToList();
    }

    public async Task<MatchDto?> GetByIdAsync(string matchId, CancellationToken ct = default)
    {
        var row = await Detailed.FirstOrDefaultAsync(m => m.Id == matchId, ct);
        return row is null ? null : EntityMap.ToDto(row);
    }

    public async Task<MatchDto> AddAsync(MatchDto match, CancellationToken ct = default)
    {
        var entity = EntityMap.ToEntity(match);
        db.Matches.Add(entity);
        await db.SaveChangesAsync(ct);
        await db.Entry(entity).Reference(m => m.HomeTeam).LoadAsync(ct);
        await db.Entry(entity).Reference(m => m.VisitorTeam).LoadAsync(ct);
        return EntityMap.ToDto(entity);
    }

    public async Task<MatchDto?> ReplaceAsync(MatchDto match, CancellationToken ct = default)
    {
        var entity = await db.Matches.FirstOrDefaultAsync(m => m.Id == match.Id, ct);
        if (entity is null) return null;

        entity.GroupId = match.GroupId;
        entity.HomeTeamId = match.HomeTeamId;
        entity.VisitorTeamId = match.VisitorTeamId;
        if (match.IsCompleted)
            entity.RecordScore(match.Score.HomeTeamScore, match.Score.VisitorTeamScore);
        else
        {
            entity.HomeScore = match.Score.HomeTeamScore;
            entity.VisitorScore = match.Score.VisitorTeamScore;
            entity.IsCompleted = false;
        }

        await db.SaveChangesAsync(ct);
        await db.Entry(entity).Reference(m => m.HomeTeam).LoadAsync(ct);
        await db.Entry(entity).Reference(m => m.VisitorTeam).LoadAsync(ct);
        return EntityMap.ToDto(entity);
    }

    public async Task<bool> RemoveAsync(string matchId, CancellationToken ct = default)
    {
        var entity = await db.Matches.FirstOrDefaultAsync(m => m.Id == matchId, ct);
        if (entity is null) return false;
        db.Matches.Remove(entity);
        await db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<int> RemoveByTournamentAsync(string tournamentId, CancellationToken ct = default)
    {
        var rows = await db.Matches.Where(m => m.TournamentId == tournamentId).ToListAsync(ct);
        db.Matches.RemoveRange(rows);
        await db.SaveChangesAsync(ct);
        return rows.Count;
    }

    public Task<int> CountByTeamAsync(string teamId, CancellationToken ct = default) =>
        db.Matches.CountAsync(m => m.HomeTeamId == teamId || m.VisitorTeamId == teamId, ct);
}

internal static class EntityMap
{
    public static TournamentDto ToDto(DomainTournament tournament) =>
        new(
            tournament.Id,
            tournament.Name,
            new TournamentFormatDto(
                tournament.MaxTeamsPerGroup,
                tournament.NumberOfGroups,
                ToDtoType(tournament.Type)),
            [],
            []);

    public static DomainTournament ToEntity(TournamentDto tournament) =>
        new()
        {
            Id = tournament.Id,
            Name = tournament.Name,
            MaxTeamsPerGroup = tournament.Format.MaxTeamsPerGroup,
            NumberOfGroups = tournament.Format.NumberOfGroups,
            Type = ToDomain(tournament.Format.Type)
        };

    public static GroupDto ToDto(DomainGroup group) =>
        new(
            group.Id,
            group.Name,
            group.TournamentId,
            group.Teams.Select(t => new TeamDto(t.Id, t.Name)).ToList());

    public static MatchDto ToDto(DomainMatch match) =>
        new(
            match.Id,
            match.TournamentId,
            match.GroupId,
            match.HomeTeamId,
            match.VisitorTeamId,
            match.HomeTeam is null ? null : new TeamDto(match.HomeTeam.Id, match.HomeTeam.Name),
            match.VisitorTeam is null ? null : new TeamDto(match.VisitorTeam.Id, match.VisitorTeam.Name),
            new ScoreDto(match.HomeScore, match.VisitorScore),
            match.Winner is null ? null : Enum.Parse<MatchWinner>(match.Winner.Value.ToString()),
            match.IsCompleted);

    public static DomainMatch ToEntity(MatchDto match) =>
        new()
        {
            Id = match.Id,
            TournamentId = match.TournamentId,
            GroupId = match.GroupId,
            HomeTeamId = match.HomeTeamId,
            VisitorTeamId = match.VisitorTeamId,
            HomeScore = match.Score.HomeTeamScore,
            VisitorScore = match.Score.VisitorTeamScore,
            IsCompleted = match.IsCompleted
        };

    public static DomainType ToDomain(TournamentType type) => Enum.Parse<DomainType>(type.ToString());

    public static TournamentType ToDtoType(DomainType type) => Enum.Parse<TournamentType>(type.ToString());
}
