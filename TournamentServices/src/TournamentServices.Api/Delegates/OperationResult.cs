namespace TournamentServices.Api.Delegates;

/// <summary>
/// Resultado de una operacion de negocio, independiente de HTTP.
/// Las rutas traducen el Status a TypedResults.
/// </summary>
public enum OperationStatus
{
    Success,
    NotFound,
    Conflict,
    Invalid
}

public record OperationResult<T>(OperationStatus Status, T? Value, string? Error)
{
    public bool IsSuccess => Status == OperationStatus.Success;

    public static OperationResult<T> Ok(T value) =>
        new(OperationStatus.Success, value, null);

    public static OperationResult<T> NotFound(string error) =>
        new(OperationStatus.NotFound, default, error);

    public static OperationResult<T> Conflict(string error) =>
        new(OperationStatus.Conflict, default, error);

    public static OperationResult<T> Invalid(string error) =>
        new(OperationStatus.Invalid, default, error);
}
