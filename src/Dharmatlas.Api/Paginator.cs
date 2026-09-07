using Dharmatlas.Api.Models;

namespace Dharmatlas.Api;

/// <summary>
/// Pure, DB-agnostic bounded pagination. The input is assumed already ordered by
/// the cursor key; the cursor is the last item's key from a previous page. The
/// requested limit is clamped to a safe range so a client cannot request an
/// unbounded page.
/// </summary>
public static class Paginator
{
    /// <summary>
    /// Returns a page of at most <paramref name="limit"/> items starting after the
    /// <paramref name="after"/> cursor. Extra items beyond the page are inspected
    /// only to compute <see cref="Paginated{T}.HasMore"/> and the next cursor.
    /// </summary>
    public static Paginated<T> Apply<T>(
        IReadOnlyList<T> ordered,
        int limit,
        string? after,
        Func<T, string> cursorKey)
    {
        var pageSize = ClampLimit(limit);
        var list = ordered as List<T> ?? ordered.ToList();

        var start = 0;
        if (after is not null)
        {
            while (start < list.Count &&
                   string.CompareOrdinal(cursorKey(list[start]), after) <= 0)
            {
                start++;
            }
        }

        var window = list.Skip(start).Take(pageSize + 1).ToList();
        var hasMore = window.Count > pageSize;
        var page = hasMore ? window.Take(pageSize).ToList() : window;
        var nextCursor = hasMore && page.Count > 0 ? cursorKey(page[^1]) : null;

        return new Paginated<T>
        {
            Items = page,
            HasMore = hasMore,
            NextCursor = nextCursor,
            Limit = pageSize
        };
    }

    /// <summary>Clamps a page-size request into [1, <see cref="ApiConstants.MaxPageSize"/>].</summary>
    public static int ClampLimit(int requested) =>
        requested < 1 ? ApiConstants.DefaultPageSize : Math.Min(requested, ApiConstants.MaxPageSize);
}
