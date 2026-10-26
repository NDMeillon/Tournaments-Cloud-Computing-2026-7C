using Microsoft.EntityFrameworkCore;
using TournamentServices.Domain;

namespace TournamentServices.Infrastructure;

public class TournamentDbContext(DbContextOptions<TournamentDbContext> options) : DbContext(options)
{
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<Tournament> Tournaments => Set<Tournament>();
    public DbSet<Group> Groups => Set<Group>();
    public DbSet<Match> Matches => Set<Match>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Team>(entity =>
        {
            entity.ToTable("teams");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasMaxLength(64);
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(t => t.Name).IsUnique();
        });

        modelBuilder.Entity<Tournament>(entity =>
        {
            entity.ToTable("tournaments");
            entity.HasKey(t => t.Id);
            entity.Property(t => t.Id).HasMaxLength(64);
            entity.Property(t => t.Name).HasMaxLength(100).IsRequired();
            entity.OwnsOne(t => t.Format, format =>
            {
            format.Property(f => f.MaxTeamsPerGroup).HasColumnName("MaxTeamsPerGroup");
            format.Property(f => f.NumberOfGroups).HasColumnName("NumberOfGroups");
            format.Property(f => f.Type).HasColumnName("Type").HasConversion<string>().HasMaxLength(32);
            });            
            entity.HasMany(t => t.Groups)
                .WithOne(g => g.Tournament)
                .HasForeignKey(g => g.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(t => t.Matches)
                .WithOne(m => m.Tournament)
                .HasForeignKey(m => m.TournamentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Group>(entity =>
        {
            entity.ToTable("groups");
            entity.HasKey(g => g.Id);
            entity.Property(g => g.Id).HasMaxLength(64);
            entity.Property(g => g.Name).HasMaxLength(100).IsRequired();
            entity.Property(g => g.TournamentId).HasMaxLength(64);
            entity.HasIndex(g => new { g.TournamentId, g.Name }).IsUnique();
            entity.HasMany(g => g.Teams)
                .WithMany(t => t.Groups)
                .UsingEntity<Dictionary<string, object>>(
                    "group_teams",
                    right => right.HasOne<Team>().WithMany().HasForeignKey("team_id").OnDelete(DeleteBehavior.Cascade),
                    left => left.HasOne<Group>().WithMany().HasForeignKey("group_id").OnDelete(DeleteBehavior.Cascade),
                    join =>
                    {
                        join.HasKey("group_id", "team_id");
                        join.ToTable("group_teams");
                    });
        });

        modelBuilder.Entity<Match>(entity =>
        {
            entity.ToTable("matches");
            entity.HasKey(m => m.Id);
            entity.Property(m => m.Id).HasMaxLength(64);
            entity.Property(m => m.TournamentId).HasMaxLength(64);
            entity.Property(m => m.GroupId).HasMaxLength(64);
            entity.Property(m => m.HomeTeamId).HasMaxLength(64);
            entity.Property(m => m.VisitorTeamId).HasMaxLength(64);
            entity.Ignore(m => m.Winner);
            entity.OwnsOne(m => m.Score, score =>
            {
                score.Property(s => s.HomeTeamScore).HasColumnName("HomeScore");
                score.Property(s => s.VisitorTeamScore).HasColumnName("VisitorScore");
            });
            entity.HasOne(m => m.Group)
                .WithMany()
                .HasForeignKey(m => m.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasOne(m => m.HomeTeam)
                .WithMany()
                .HasForeignKey(m => m.HomeTeamId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(m => m.VisitorTeam)
                .WithMany()
                .HasForeignKey(m => m.VisitorTeamId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
