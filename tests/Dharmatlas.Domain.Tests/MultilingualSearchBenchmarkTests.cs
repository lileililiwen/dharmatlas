using System.Diagnostics;
using System.Text.Json;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Search.Engine;
using Dharmatlas.Search.Models;
using Xunit;
using Xunit.Abstractions;

namespace Dharmatlas.Domain.Tests;

/// <summary>
/// Checked-in multilingual benchmark: every fixture case must resolve to its
/// canonical entity, and the run reports p50/p95 latency at limit=100 against
/// the 250ms search-engine adoption gate. Queries are synthetic and the run
/// logs no identity; the fixture measures the engine, not users.
/// </summary>
public sealed class MultilingualSearchBenchmarkTests
{
    private const double AdoptionGateP95Ms = 250.0;
    private const int DistractorCount = 500;

    private readonly ITestOutputHelper _output;

    public MultilingualSearchBenchmarkTests(ITestOutputHelper output) => _output = output;

    [Fact]
    public void Fixture_passes_100_percent_and_p95_meets_adoption_gate()
    {
        var fixture = LoadFixture();
        var entities = BuildEntities(fixture);
        var latencies = new List<double>(fixture.Cases.Count);
        var failures = new List<string>();

        foreach (var @case in fixture.Cases)
        {
            var timer = Stopwatch.StartNew();
            var result = SearchEngine.Search(
                new SearchQuery { Term = @case.Query, Limit = SearchEngine.MaxResults }, entities);
            timer.Stop();
            latencies.Add(timer.Elapsed.TotalMilliseconds);

            var rank = result.Hits
                .Select((hit, index) => (hit, rank: index + 1))
                .FirstOrDefault(x => fixture.IdOf(x.hit.Id) == @case.ExpectedId)
                .rank;
            if (rank == 0 || rank > @case.MinRank)
            {
                failures.Add($"'{@case.Query}' expected '{@case.ExpectedId}' within rank {@case.MinRank} but found rank {rank}.");
            }
        }

        latencies.Sort();
        var p50 = Percentile(latencies, 50);
        var p95 = Percentile(latencies, 95);
        _output.WriteLine($"multilingual benchmark: {fixture.Cases.Count} cases, {entities.Count} entities, limit=100");
        _output.WriteLine($"pass rate: {fixture.Cases.Count - failures.Count}/{fixture.Cases.Count}");
        _output.WriteLine($"p50: {p50:F2} ms, p95: {p95:F2} ms, adoption gate p95 <= {AdoptionGateP95Ms:F0} ms");
        foreach (var failure in failures)
        {
            _output.WriteLine("FAIL: " + failure);
        }

        Assert.Empty(failures);
        Assert.True(p95 <= AdoptionGateP95Ms, $"p95 {p95:F2} ms exceeds the {AdoptionGateP95Ms:F0} ms adoption gate.");
    }

    [Fact]
    public void Ambiguous_fixture_term_stays_separate_hits()
    {
        var fixture = LoadFixture();
        var entities = BuildEntities(fixture);

        foreach (var ambiguous in fixture.Ambiguous)
        {
            var result = SearchEngine.Search(
                new SearchQuery { Term = ambiguous.Query, Limit = SearchEngine.MaxResults }, entities);
            var ids = result.Hits.Select(h => fixture.IdOf(h.Id)).ToList();
            foreach (var expected in ambiguous.ExpectedIds)
            {
                Assert.Contains(expected, ids);
            }

            Assert.All(result.Hits, h =>
            {
                Assert.False(string.IsNullOrWhiteSpace(h.MatchedName));
                Assert.False(string.IsNullOrWhiteSpace(h.CanonicalName));
            });
        }
    }

    private static double Percentile(List<double> sorted, double percentile)
    {
        if (sorted.Count == 0)
        {
            return 0;
        }

        var rank = (percentile / 100.0) * (sorted.Count - 1);
        var lower = (int)Math.Floor(rank);
        var upper = (int)Math.Ceiling(rank);
        return lower == upper ? sorted[lower] : sorted[lower] + (sorted[upper] - sorted[lower]) * (rank - lower);
    }

    private Fixture LoadFixture()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "fixtures", "multilingual.json");
        Assert.True(File.Exists(path), "Benchmark fixture missing: " + path);
        using var stream = File.OpenRead(path);
        var document = JsonSerializer.Deserialize<FixtureDocument>(
            stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(document);
        return new Fixture(document!);
    }

    private List<SearchEntity> BuildEntities(Fixture fixture)
    {
        var entities = new List<SearchEntity>();
        foreach (var entity in fixture.Document.Entities)
        {
            var id = fixture.KeyOf(entity.Id);
            var names = new List<EntityName>
            {
                new(id, "eng", "latin", string.Empty, entity.Canonical, true)
            };
            names.AddRange(entity.Aliases.Select(a =>
                new EntityName(id, a.Language, a.Script, a.Romanization, a.Value)));
            entities.Add(new SearchEntity
            {
                Id = id,
                Type = Enum.Parse<EntityType>(entity.Type, true),
                Names = names
            });
        }

        for (var i = 0; i < DistractorCount; i++)
        {
            var id = EntityId.New();
            entities.Add(new SearchEntity
            {
                Id = id,
                Type = EntityType.Person,
                Names = new List<EntityName> { new(id, "eng", "latin", string.Empty, $"Distractor {i:000}") }
            });
        }

        return entities;
    }

    private sealed class Fixture
    {
        private readonly Dictionary<EntityId, string> _ids = new();
        private readonly Dictionary<string, EntityId> _keys = new();

        public Fixture(FixtureDocument document) => Document = document;

        public FixtureDocument Document { get; }

        public IReadOnlyList<FixtureCase> Cases => Document.Cases;

        public IReadOnlyList<FixtureAmbiguous> Ambiguous => Document.Ambiguous;

        public EntityId KeyOf(string key)
        {
            if (!_keys.TryGetValue(key, out var id))
            {
                id = EntityId.New();
                _keys[key] = id;
                _ids[id] = key;
            }

            return id;
        }

        public string? IdOf(EntityId id) =>
            _ids.TryGetValue(id, out var key) ? key : null;
    }

    private sealed record FixtureDocument(
        List<FixtureEntity> Entities,
        List<FixtureCase> Cases,
        List<FixtureAmbiguous> Ambiguous);

    private sealed record FixtureEntity(
        string Id,
        string Canonical,
        string Type,
        List<FixtureAlias> Aliases);

    private sealed record FixtureAlias(
        string Value,
        string Language,
        string Script,
        string Romanization);

    private sealed record FixtureCase(
        string Query,
        string ExpectedId,
        int MinRank,
        string Note);

    private sealed record FixtureAmbiguous(
        string Query,
        List<string> ExpectedIds,
        string Note);
}
