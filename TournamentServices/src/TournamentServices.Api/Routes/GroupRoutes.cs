using TournamentServices.Api.Delegates;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class GroupRoutes
{
    public static RouteGroupBuilder MapGroupRoutes(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/tournaments/{tournamentId}/groups")
                       .WithTags("Groups");

        group.MapGet("", GetGroups);
        group.MapGet("/{groupId}", GetGroupById);
        group.MapPost("", CreateGroup)
             .AddEndpointFilter<ValidationFilter<CreateGroupDto>>();
        group.MapPut("/{groupId}", UpdateGroup)
             .AddEndpointFilter<ValidationFilter<UpdateGroupDto>>();
        group.MapDelete("/{groupId}", DeleteGroup);
        group.MapPatch("/{groupId}/teams", AssignTeams)
             .AddEndpointFilter<ValidationFilter<AssignTeamsDto>>();

        return group;
    }

    private static async Task<IResult> GetGroups(string tournamentId, IGroupDelegate groups, CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(await groups.GetByTournamentAsync(tournamentId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> GetGroupById(
        string tournamentId,
        string groupId,
        IGroupDelegate groups,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(groupId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await groups.GetByIdAsync(tournamentId, groupId, ct), TypedResults.Ok);
    }

    private static async Task<IResult> CreateGroup(
        string tournamentId,
        CreateGroupDto request,
        IGroupDelegate groups,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId))
            return TypedResults.BadRequest("Invalid Tournament ID format.");

        return RouteSupport.From(
            await groups.CreateAsync(tournamentId, request, ct),
            group => TypedResults.Created($"/tournaments/{tournamentId}/groups/{group.Id}", group));
    }

    private static async Task<IResult> UpdateGroup(
        string tournamentId,
        string groupId,
        UpdateGroupDto request,
        IGroupDelegate groups,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(groupId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await groups.UpdateAsync(tournamentId, groupId, request, ct), TypedResults.Ok);
    }

    private static async Task<IResult> DeleteGroup(
        string tournamentId,
        string groupId,
        IGroupDelegate groups,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(groupId))
            return TypedResults.BadRequest("Invalid ID format.");

        return RouteSupport.From(await groups.DeleteAsync(tournamentId, groupId, ct), _ => TypedResults.NoContent());
    }

    private static async Task<IResult> AssignTeams(
        string tournamentId,
        string groupId,
        AssignTeamsDto request,
        IGroupDelegate groups,
        CancellationToken ct)
    {
        if (!RouteSupport.IsValidId(tournamentId) || !RouteSupport.IsValidId(groupId))
            return TypedResults.BadRequest("Invalid ID format in route parameters.");

        return RouteSupport.From(await groups.AssignTeamsAsync(tournamentId, groupId, request, ct), _ => TypedResults.NoContent());
    }
}
