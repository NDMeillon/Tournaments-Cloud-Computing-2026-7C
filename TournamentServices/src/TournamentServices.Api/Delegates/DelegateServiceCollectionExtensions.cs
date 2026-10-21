using TournamentServices.Api.Repositories;

namespace TournamentServices.Api.Delegates;

public static class DelegateServiceCollectionExtensions
{
    /// <summary>
    /// Registra la capa de delegados y los repositorios de EF Core.
    /// El DbContext se registra en Program.cs.
    /// </summary>
    public static IServiceCollection AddDelegateLayer(this IServiceCollection services)
    {
        services.AddScoped<ITournamentRepository, EfTournamentRepository>();
        services.AddScoped<ITeamRepository, EfTeamRepository>();
        services.AddScoped<IGroupRepository, EfGroupRepository>();
        services.AddScoped<IMatchRepository, EfMatchRepository>();
        services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        // Delegados: uno por request.
        services.AddScoped<ITournamentDelegate, TournamentDelegate>();
        services.AddScoped<ITeamDelegate, TeamDelegate>();
        services.AddScoped<IGroupDelegate, GroupDelegate>();
        services.AddScoped<IMatchDelegate, MatchDelegate>();

        return services;
    }
}
