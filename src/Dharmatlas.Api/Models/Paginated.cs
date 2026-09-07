namespace Dharmatlas.Api.Models;

/// <summary>
/// A bounded page of results plus navigation metadata. <see cref="HasMore"/>
/// indicates whether a further page exists and <see cref="NextCursor"/> carries the
/// cursor to pass as <see cref="Paging.After"/> to fetch it.
/// </summary>
public sealed record Paginated<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public bool HasMore { get; init; }
    public string? NextCursor { get; init; }
    public int Limit { get; init; }
}
