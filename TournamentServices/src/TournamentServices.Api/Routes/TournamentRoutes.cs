namespace TournamentServices.Api.Routes;

using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

public static class TournamentRoutes
{
    public static RouteGroupBuilder MapTournamentRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/tournaments").WithTags("Tournaments");
        // Existing mappings remain identical
        return group;
    }
}