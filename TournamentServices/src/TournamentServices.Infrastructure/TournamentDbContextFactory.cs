using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TournamentServices.Infrastructure;

public class TournamentDbContextFactory : IDesignTimeDbContextFactory<TournamentDbContext>
{
    public const string LocalConnection =
        "Host=localhost;Port=5432;Database=tournament_db;Username=tournament_svc;Password=password";

    public TournamentDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<TournamentDbContext>()
            .UseNpgsql(LocalConnection)
            .Options;

        return new TournamentDbContext(options);
    }
}
