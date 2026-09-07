namespace Dharmatlas.Api.Models;

/// <summary>
/// A typed API error returned by handlers. Serialized into the response body
/// alongside the HTTP status code.
/// </summary>
public sealed record ApiError
{
    public int Status { get; init; }
    public string Code { get; init; }
    public string Message { get; init; }
    public string? Detail { get; init; }

    public ApiError(int status, string code, string message, string? detail = null)
    {
        Status = status;
        Code = code;
        Message = message;
        Detail = detail;
    }
}
