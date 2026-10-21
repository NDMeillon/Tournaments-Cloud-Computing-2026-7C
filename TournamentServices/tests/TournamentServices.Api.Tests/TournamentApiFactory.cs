using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TournamentServices.Infrastructure;

namespace TournamentServices.Api.Tests;

public class TournamentApiFactory : WebApplicationFactory<Program>
{
    private readonly string _connection =
        $"Data Source={Path.Combine(Path.GetTempPath(), $"tournaments-{Guid.NewGuid():N}.db")}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("SqliteConnection", _connection);
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["SqliteConnection"] = _connection
            });
        });
    }

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TournamentDbContext>();
        await db.Database.EnsureDeletedAsync();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        TryDeleteDatabaseFile();
    }

    private void TryDeleteDatabaseFile()
    {
        var path = _connection["Data Source=".Length..];
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch (IOException)
        {
            // El host puede seguir con el archivo abierto al cerrar la fabrica.
        }
    }
}
