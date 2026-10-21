using System.Text.RegularExpressions;
using TournamentServices.Api.Delegates;

namespace TournamentServices.Api.Routes;

internal static class RouteSupport
{
    private static readonly Regex IdPattern = new(@"^[A-Za-z0-9\-]+$", RegexOptions.Compiled);

    public static bool IsValidId(string? id) => id is not null && IdPattern.IsMatch(id);

    public static IResult From<T>(OperationResult<T> result, Func<T, IResult> onSuccess)
    {
        return result.Status switch
        {
            OperationStatus.Success => onSuccess(result.Value!),
            OperationStatus.NotFound => TypedResults.NotFound(),
            OperationStatus.Conflict => TypedResults.UnprocessableEntity(result.Error ?? "Business rule violated."),
            _ => TypedResults.BadRequest(result.Error ?? "Invalid request.")
        };
    }
}
