using Dharmatlas.Search.Models;

namespace Dharmatlas.Search.Services;

/// <summary>
/// Read-only multilingual entity search contract. Independent of any UI.
/// </summary>
public interface ISearchQueryService
{
    Task<SearchResult> QueryAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
