using Dharmatlas.Api.Models;

namespace Dharmatlas.Api;

/// <summary>
/// Carries a typed API error. The endpoint mapping layer translates this into an
/// HTTP status code and an RFC 7807-style problem payload, so handlers stay free
/// of ASP.NET types and remain unit testable.
/// </summary>
public sealed class ApiException : Exception
{
    public ApiError Error { get; }

    public ApiException(ApiError error) : base(error.Message) => Error = error;

    public static ApiException NotFound(string id) =>
        new(new ApiError(404, "not_found", $"No published record was found with id '{id}'."));

    public static ApiException BadRequest(string message, string? detail = null) =>
        new(new ApiError(400, "bad_request", message, detail));

    public static ApiException TooManyRequests(int retryAfterSeconds) =>
        new(new ApiError(429, "rate_limited", "Too many requests.", $"Retry after {retryAfterSeconds} seconds."));
}
