using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public static class DelegateServiceCollectionExtensions
{
    /// <summary>
    /// Registra la capa de delegados y su persistencia.
    /// Llamar desde Program.cs: builder.Services.AddDelegateLayer();
    /// </summary>
    public static IServiceCollection AddDelegateLayer(this IServiceCollection services)
    {
        // Almacen compartido: Singleton mientras sea in-memory.
        services.AddSingleton<InMemoryStore>();

        // Repositorios.
        services.AddScoped<ITournamentRepository, InMemoryTournamentRepository>();
        services.AddScoped<ITeamRepository, InMemoryTeamRepository>();
        services.AddScoped<IGroupRepository, InMemoryGroupRepository>();
        services.AddScoped<IMatchRepository, InMemoryMatchRepository>();
        services.AddScoped<IUnitOfWork, InMemoryUnitOfWork>();

        // Delegados: uno por request.
        services.AddScoped<ITournamentDelegate, TournamentDelegate>();
        services.AddScoped<ITeamDelegate, TeamDelegate>();
        services.AddScoped<IGroupDelegate, GroupDelegate>();
        services.AddScoped<IMatchDelegate, MatchDelegate>();

        return services;
    }
}
