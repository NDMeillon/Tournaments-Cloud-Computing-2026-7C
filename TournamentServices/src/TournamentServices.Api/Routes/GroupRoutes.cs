using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http.HttpResults;
using TournamentServices.Api.Dtos;
using TournamentServices.Api.Extensions;

namespace TournamentServices.Api.Routes;

public static class GroupRoutes
{
    private const string IdPattern = @"^[A-Za-z0-9\-]+$";

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

    private static Results<Ok<List<GroupDto>>, BadRequest<string>> GetGroups(string tournamentId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var stubs = new List<GroupDto>
        {
            new("group-a", "Grupo A", tournamentId, []),
            new("group-b", "Grupo B", tournamentId, [])
        };

        return TypedResults.Ok(stubs);
    }

    private static Results<Ok<GroupDto>, BadRequest<string>, NotFound> GetGroupById(string tournamentId, string groupId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(groupId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        var stub = new GroupDto(groupId, "Grupo A", tournamentId, []);
        return TypedResults.Ok(stub);
    }

    private static Results<Created<GroupDto>, BadRequest<string>, UnprocessableEntity<string>> CreateGroup(
        string tournamentId,
        CreateGroupDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid Tournament ID format.");
        }

        var generatedId = $"grp-{Guid.NewGuid().ToString("N")[..8]}";
        var stub = new GroupDto(generatedId, request.Name, tournamentId, []);

        return TypedResults.Created($"/tournaments/{tournamentId}/groups/{stub.Id}", stub);
    }

    private static Results<Ok<GroupDto>, BadRequest<string>, NotFound> UpdateGroup(
        string tournamentId,
        string groupId,
        UpdateGroupDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(groupId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        var stub = new GroupDto(groupId, request.Name, tournamentId, []);
        return TypedResults.Ok(stub);
    }

    private static Results<NoContent, BadRequest<string>, NotFound> DeleteGroup(string tournamentId, string groupId)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(groupId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format.");
        }

        return TypedResults.NoContent();
    }

    private static Results<NoContent, BadRequest<string>, UnprocessableEntity<string>> AssignTeams(
        string tournamentId,
        string groupId,
        AssignTeamsDto request)
    {
        if (!Regex.IsMatch(tournamentId, IdPattern) || !Regex.IsMatch(groupId, IdPattern))
        {
            return TypedResults.BadRequest("Invalid ID format in route parameters.");
        }

        return TypedResults.NoContent();
    }
}