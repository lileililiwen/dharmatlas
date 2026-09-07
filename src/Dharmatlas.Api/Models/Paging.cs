namespace Dharmatlas.Api.Models;

/// <summary>
/// Bounded pagination parameters for list endpoints. <see cref="After"/> is an
/// opaque cursor (the last item's stable id from a prior page); it keeps paging
/// stable even if new records are inserted.
/// </summary>
public sealed record Paging(int Limit, string? After);
