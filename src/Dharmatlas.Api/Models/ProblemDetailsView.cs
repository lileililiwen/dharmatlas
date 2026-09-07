namespace Dharmatlas.Api.Models;

/// <summary>
/// RFC 7807-style problem payload returned for error responses. Kept distinct from
/// ASP.NET's ProblemDetails so it can be produced and asserted in handler tests
/// without booting a server.
/// </summary>
public sealed record ProblemDetailsView
{
    public int Status { get; init; }
    public required string Code { get; init; }
    public required string Title { get; init; }
    public required string Detail { get; init; }

    public static ProblemDetailsView From(ApiError error) => new()
    {
        Status = error.Status,
        Code = error.Code,
        Title = error.Code.Replace('_', ' '),
        Detail = error.Detail ?? error.Message
    };
}
