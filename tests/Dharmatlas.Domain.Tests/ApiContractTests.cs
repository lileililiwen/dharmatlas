using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Text.Json;
using Dharmatlas.Api;
using Dharmatlas.Api.Models;
using Dharmatlas.Api.RateLimit;
using Dharmatlas.Api.Services;
using Dharmatlas.Domain;
using Dharmatlas.Domain.Entities;
using Dharmatlas.Domain.ValueObjects;
using Dharmatlas.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Dharmatlas.Domain.Tests;

[SuppressMessage("ReSharper", "AccessToDisposedClosure")]
public class ApiContractTests
{
    private static EntityName Name(EntityId owner, string value, bool primary = false, string script = "latin") =>
        new(owner, "eng", script, string.Empty, value, primary);

    // --- Single-entity endpoints surface metadata (names, certainty, sources) ---

    [Fact]
    public async Task Person_endpoint_surfaces_names_relationships_and_sources()
    {
        var db = NewDb();
        var personId = EntityId.New();
        var placeId = EntityId.New();
        var sourceId = EntityId.New();

        db.Entities.Add(new Person { Id = personId });
        db.Entities.Add(new Place { Id = placeId, Region = "India" });
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true, script: "han"));
        db.EntityNames.Add(Name(personId, "Hsuan-tsang"));
        db.Sources.Add(new Source("Great Tang Records") { Id = sourceId });
        db.Relationships.Add(new Relationship(personId, placeId, "visited", Certainty.Documented, new[] { sourceId }));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var person = await service.GetPersonAsync(personId);

        Assert.NotNull(person);
        Assert.Equal("Xuanzang", person!.CanonicalName);
        Assert.Equal(2, person.Names.Count);
        Assert.Single(person.Relationships, r => r.RelationType == "visited" && r.Certainty == Certainty.Documented.ToString());
        Assert.Single(person.Sources, s => s.Title == "Great Tang Records");
        Assert.Equal(Certainty.Unknown.ToString(), person.Certainty);
    }

    [Fact]
    public async Task Event_endpoint_includes_date_bounds_location_participants_and_sources()
    {
        var db = NewDb();
        var eventId = EntityId.New();
        var personId = EntityId.New();
        var placeId = EntityId.New();
        var sourceId = EntityId.New();

        db.Entities.Add(new Event
        {
            Id = eventId,
            PlaceId = placeId,
            Category = "translation",
            Region = "India",
            Certainty = Certainty.Documented,
            When = HistoricalDate.Exact("645 CE", 645)
        });
        db.Entities.Add(new Person { Id = personId });
        db.Entities.Add(new Place { Id = placeId, Region = "India" });
        db.EntityNames.Add(Name(eventId, "Sutra translation", primary: true));
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true));
        db.EntityNames.Add(Name(placeId, "Nalanda", primary: true));
        db.Sources.Add(new Source("Record") { Id = sourceId });
        db.Relationships.Add(new Relationship(personId, eventId, "participated-in", Certainty.TraditionalAccount, new[] { sourceId }));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var ev = await service.GetEventAsync(eventId);

        Assert.NotNull(ev);
        Assert.Equal("645 CE", ev!.When!.DisplayExpression);
        Assert.Equal(645, ev.When.LowerBound);
        Assert.Equal(645, ev.When.UpperBound);
        Assert.Equal("Nalanda", ev.Place!.CanonicalName);
        Assert.Equal("translation", ev.Category);
        Assert.Equal(Certainty.Documented.ToString(), ev.Certainty);
        Assert.Single(ev.Participants, p => p.CanonicalName == "Xuanzang");
        Assert.Single(ev.Sources);
    }

    [Fact]
    public async Task Place_endpoint_includes_geography_kind_activity_and_sources()
    {
        var db = NewDb();
        var placeId = EntityId.New();
        var sourceId = EntityId.New();

        db.Entities.Add(new Place
        {
            Id = placeId,
            Region = "India",
            Kind = PlaceKind.Monastery,
            Latitude = 25.0,
            Longitude = 85.0,
            ModernName = "Baragaon",
            Certainty = Certainty.Documented,
            Activity = HistoricalDate.Approximate("5th century", 400, 500)
        });
        db.EntityNames.Add(Name(placeId, "Nalanda", primary: true));
        db.Sources.Add(new Source("Archaeology") { Id = sourceId });
        db.Entities.Add(new Person { Id = EntityId.New() });
        db.Relationships.Add(new Relationship(EntityId.New(), placeId, "founded", Certainty.Probable, new[] { sourceId }));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var place = await service.GetPlaceAsync(placeId);

        Assert.NotNull(place);
        Assert.Equal("Monastery", place!.Kind);
        Assert.Equal(25.0, place.Latitude);
        Assert.Equal("Baragaon", place.ModernName);
        Assert.Equal("5th century", place.Activity!.DisplayExpression);
        Assert.Equal(Certainty.Documented.ToString(), place.Certainty);
        Assert.Single(place.Sources);
    }

    [Fact]
    public async Task Text_endpoint_includes_language_and_sources()
    {
        var db = NewDb();
        var textId = EntityId.New();
        var authorId = EntityId.New();
        var sourceId = EntityId.New();

        db.Entities.Add(new Text { Id = textId, OriginalLanguage = "san" });
        db.Entities.Add(new Person { Id = authorId });
        db.EntityNames.Add(Name(textId, "Heart Sutra", primary: true));
        db.Sources.Add(new Source("Canon") { Id = sourceId });
        db.Relationships.Add(new Relationship(authorId, textId, "authored", Certainty.Documented, new[] { sourceId }));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var text = await service.GetTextAsync(textId);

        Assert.NotNull(text);
        Assert.Equal("san", text!.OriginalLanguage);
        Assert.Single(text.Sources, s => s.Title == "Canon");
    }

    [Fact]
    public async Task Unknown_id_returns_null_for_entity_endpoints()
    {
        var db = NewDb();
        var service = new ApiQueryService(db);

        Assert.Null(await service.GetPersonAsync(EntityId.New()));
        Assert.Null(await service.GetEventAsync(EntityId.New()));
        Assert.Null(await service.GetPlaceAsync(EntityId.New()));
        Assert.Null(await service.GetTextAsync(EntityId.New()));
        Assert.Null(await service.GetSourceAsync(EntityId.New()));
    }

    // --- List endpoints, filters, and bounded pagination ---

    [Fact]
    public async Task ListEvents_filters_by_year_range_and_region()
    {
        var db = NewDb();
        var inRange = EntityId.New();
        var outRange = EntityId.New();

        db.Entities.Add(new Event { Id = inRange, Region = "India", When = HistoricalDate.Exact("600 CE", 600) });
        db.Entities.Add(new Event { Id = outRange, Region = "China", When = HistoricalDate.Exact("1200 CE", 1200) });
        db.EntityNames.Add(Name(inRange, "A", primary: true));
        db.EntityNames.Add(Name(outRange, "B", primary: true));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var page = await service.ListEventsAsync(new EventQuery(550, 650, "India", null, null, new Paging(20, null)));

        Assert.Single(page.Items);
        Assert.Equal(inRange.ToString(), page.Items[0].Id);
    }

    [Fact]
    public async Task ListRelationships_filters_by_type_and_min_certainty()
    {
        var db = NewDb();
        var a = EntityId.New();
        var b = EntityId.New();
        var c = EntityId.New();
        var s = EntityId.New();

        db.Entities.Add(new Person { Id = a });
        db.Entities.Add(new Person { Id = b });
        db.Entities.Add(new Person { Id = c });
        db.Sources.Add(new Source("X") { Id = s });
        db.Relationships.Add(new Relationship(a, b, "teacher-of", Certainty.Documented, new[] { s }));
        db.Relationships.Add(new Relationship(a, c, "influenced", Certainty.Disputed, new[] { s }));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);

        // Documented is the lowest certainty, so a Documented floor returns both.
        var documented = await service.ListRelationshipsAsync(
            new RelationshipQuery(null, null, null, Certainty.Documented, new Paging(20, null)));
        Assert.Equal(2, documented.Items.Count);

        // A higher minimum certainty excludes the lower-certainty (Documented) relationship.
        var disputedFloor = await service.ListRelationshipsAsync(
            new RelationshipQuery(null, null, null, Certainty.Disputed, new Paging(20, null)));
        Assert.Single(disputedFloor.Items);
        Assert.Equal("influenced", disputedFloor.Items[0].Type);

        // Type filter narrows to a single relationship.
        var byType = await service.ListRelationshipsAsync(
            new RelationshipQuery(null, null, "teacher-of", null, new Paging(20, null)));
        Assert.Single(byType.Items);
        Assert.Equal("teacher-of", byType.Items[0].Type);
    }

    [Fact]
    public async Task Pagination_pages_through_persons_with_cursor()
    {
        var db = NewDb();
        for (var i = 0; i < 5; i++)
        {
            var id = EntityId.New();
            db.Entities.Add(new Person { Id = id });
            db.EntityNames.Add(Name(id, $"P{i}", primary: true));
        }

        await db.SaveChangesAsync();
        var service = new ApiQueryService(db);

        var page1 = await service.ListPersonsAsync(new PersonQuery(null, new Paging(2, null)));
        Assert.Equal(2, page1.Items.Count);
        Assert.True(page1.HasMore);
        Assert.NotNull(page1.NextCursor);

        var page2 = await service.ListPersonsAsync(new PersonQuery(null, new Paging(2, page1.NextCursor)));
        Assert.Equal(2, page2.Items.Count);
        Assert.True(page2.HasMore);
        Assert.DoesNotContain(page2.Items, i => page1.Items.Any(p => p.Id == i.Id));

        var page3 = await service.ListPersonsAsync(new PersonQuery(null, new Paging(2, page2.NextCursor)));
        Assert.Single(page3.Items);
        Assert.False(page3.HasMore);
    }

    [Fact]
    public void Paginator_clamps_limit_and_pages_with_cursor()
    {
        var items = Enumerable.Range(0, 10).Select(i => $"id-{i:D2}").ToList();

        Assert.Equal(ApiConstants.DefaultPageSize, Paginator.ClampLimit(0));
        Assert.Equal(ApiConstants.MaxPageSize, Paginator.ClampLimit(10_000));
        Assert.Equal(3, Paginator.ClampLimit(3));

        var first = Paginator.Apply(items, 3, null, x => x);
        Assert.Equal(3, first.Items.Count);
        Assert.True(first.HasMore);
        Assert.Equal("id-02", first.NextCursor);

        var second = Paginator.Apply(items, 3, first.NextCursor, x => x);
        Assert.Equal(3, second.Items.Count);
        Assert.Equal("id-03", second.Items[0]);
    }

    // --- Published-data boundary ---

    [Fact]
    public async Task Pending_submission_is_excluded_from_public_api_and_export()
    {
        var db = NewDb();
        var personId = EntityId.New();
        var contributorId = EntityId.New();
        var sourceId = EntityId.New();

        // A published person that should appear.
        db.Entities.Add(new Person { Id = personId });
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true));
        db.Sources.Add(new Source("Record") { Id = sourceId });

        // A contributor (private data) and a pending submission that is NOT applied
        // to the published tables (only approval writes entities).
        db.Contributors.Add(new Contributor("Secret Reviewer") { Id = contributorId, Email = "secret@example.com" });
        db.Submissions.Add(new Submission(
            contributorId,
            SubmissionType.Name,
            "Propose new name",
            "{\"name\":\"Not Yet Published\"}",
            new[] { sourceId },
            null));
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var persons = await service.ListPersonsAsync(new PersonQuery(null, new Paging(100, null)));
        Assert.Single(persons.Items);
        Assert.DoesNotContain(persons.Items, p => p.CanonicalName == "Not Yet Published");

        var export = await service.BuildExportAsync(DateTimeOffset.UnixEpoch);
        var json = JsonSerializer.Serialize(export);
        Assert.DoesNotContain("secret@example.com", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Not Yet Published", json, StringComparison.Ordinal);
        Assert.Single(export.Entities);
    }

    [Fact]
    public async Task Rejected_submission_event_does_not_appear_in_events_list()
    {
        var db = NewDb();
        var contributorId = EntityId.New();
        var sourceId = EntityId.New();

        db.Entities.Add(new Person { Id = EntityId.New() });
        db.Sources.Add(new Source("X") { Id = sourceId });
        db.Contributors.Add(new Contributor("C") { Id = contributorId });
        var rejected = new Submission(contributorId, SubmissionType.Event, "Proposed event",
            "{\"name\":\"Never Approved\"}", new[] { sourceId }, null);
        rejected = rejected.Submit();
        db.Submissions.Add(rejected with { Status = SubmissionStatus.Rejected });
        await db.SaveChangesAsync();

        var service = new ApiQueryService(db);
        var events = await service.ListEventsAsync(new EventQuery(null, null, null, null, null, new Paging(100, null)));
        Assert.Empty(events.Items);
    }

    // --- Stable export ---

    [Fact]
    public void Export_snapshot_carries_schema_version_license_and_stable_ids()
    {
        var db = NewDb();
        var personId = EntityId.New();
        var sourceId = EntityId.New();
        db.Entities.Add(new Person { Id = personId });
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true));
        db.Sources.Add(new Source("Record") { Id = sourceId });
        db.SaveChanges();

        var snapshot = BulkExporter.Build(
            db.Entities.Local.ToList(),
            db.EntityNames.Local.ToList(),
            db.Relationships.Local.ToList(),
            db.Sources.Local.ToList(),
            DateTimeOffset.UnixEpoch);

        Assert.Equal(ApiConstants.SchemaVersion, snapshot.SchemaVersion);
        Assert.NotEmpty(snapshot.Checksum);
        Assert.Equal(ApiConstants.License, snapshot.License);
        Assert.Equal(ApiConstants.LicenseUrl, snapshot.LicenseUrl);
        Assert.Equal(1, snapshot.Index.Entities);
        Assert.Single(snapshot.Entities, e => e.Id == personId.ToString());
        Assert.Single(snapshot.Sources, s => s.Id == sourceId.ToString());
    }

    [Fact]
    public void Export_is_reproducible_for_identical_data()
    {
        var db = NewDb();
        var p1 = EntityId.New();
        var p2 = EntityId.New();
        db.Entities.Add(new Person { Id = p1 });
        db.Entities.Add(new Person { Id = p2 });
        db.EntityNames.Add(Name(p1, "A", primary: true));
        db.EntityNames.Add(Name(p2, "B", primary: true));
        db.SaveChanges();

        var snapshot1 = BulkExporter.Build(
            db.Entities.Local.ToList(), db.EntityNames.Local.ToList(),
            db.Relationships.Local.ToList(), db.Sources.Local.ToList(), DateTimeOffset.UnixEpoch);
        var snapshot2 = BulkExporter.Build(
            db.Entities.Local.ToList(), db.EntityNames.Local.ToList(),
            db.Relationships.Local.ToList(), db.Sources.Local.ToList(), DateTimeOffset.UnixEpoch);

        var json1 = JsonSerializer.Serialize(snapshot1);
        var json2 = JsonSerializer.Serialize(snapshot2);
        Assert.Equal(json1, json2);

        // Ordering is by stable id, so the first entity is deterministic regardless
        // of insertion order. With two random ids, the smaller one leads.
        Assert.Equal(new[] { p1, p2 }.Min(id => id.ToString()), snapshot1.Entities[0].Id);
    }

    [Fact]
    public async Task Export_delivery_writes_valid_gzip_without_partial_json()
    {
        var db = NewDb();
        var personId = EntityId.New();
        db.Entities.Add(new Person { Id = personId });
        db.EntityNames.Add(Name(personId, "Xuanzang", primary: true));
        await db.SaveChangesAsync();

        var snapshot = await new ApiQueryService(db).BuildExportAsync(DateTimeOffset.UnixEpoch);
        await using var compressed = new MemoryStream();
        await ExportDelivery.WriteCompressedAsync(compressed, snapshot);

        compressed.Position = 0;
        await using var gzip = new GZipStream(compressed, CompressionMode.Decompress);
        var restored = await JsonSerializer.DeserializeAsync<ExportSnapshot>(gzip, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        Assert.NotNull(restored);
        Assert.Equal(snapshot.Index.Entities, restored!.Index.Entities);
        Assert.Equal(snapshot.Index.Relationships, restored.Index.Relationships);
        Assert.Equal(snapshot.Index.Sources, restored.Index.Sources);
        Assert.Equal(snapshot.Index.Claims, restored.Index.Claims);
        Assert.Contains(restored.Entities, e => e.Id == personId.ToString());
    }

    [Fact]
    public void Export_job_clears_partial_delivery_on_failure_and_can_retry()
    {
        var job = ExportJob.Create("export-1").Start().Fail("storage unavailable");
        Assert.Equal(ExportJobStatus.Failed, job.Status);
        Assert.Null(job.DownloadUrl);
        Assert.Equal(1, job.Attempts);

        var retry = job.Start();
        Assert.Equal(ExportJobStatus.Running, retry.Status);
        Assert.Equal(2, retry.Attempts);
        var completed = retry.Complete("abc", "/api/v1/export/jobs/export-1/download");
        Assert.Equal(ExportJobStatus.Completed, completed.Status);
        Assert.Equal("abc", completed.Checksum);
    }

    // --- Rate limiting and cache/error shaping ---

    [Fact]
    public void RateLimiter_allows_within_window_and_blocks_beyond()
    {
        var limiter = new RateLimiter(limit: 2, window: TimeSpan.FromMinutes(1));
        var now = DateTimeOffset.UnixEpoch;

        Assert.True(limiter.Allow("client", now));
        Assert.True(limiter.Allow("client", now));
        Assert.False(limiter.Allow("client", now));
        Assert.Equal(0, limiter.Remaining("client", now));

        // A different client is unaffected.
        Assert.True(limiter.Allow("other", now));
    }

    [Fact]
    public void ApiException_maps_to_problem_details()
    {
        var notFound = ApiException.NotFound("abc-123");
        Assert.Equal(404, notFound.Error.Status);

        var problem = ProblemDetailsView.From(notFound.Error);
        Assert.Equal(404, problem.Status);
        Assert.Equal("not_found", problem.Code);
        Assert.Contains("abc-123", problem.Detail, StringComparison.Ordinal);

        var bad = ApiException.BadRequest("bad id");
        Assert.Equal(400, bad.Error.Status);
        Assert.Equal(400, ProblemDetailsView.From(bad.Error).Status);
    }

    [Fact]
    public void Meta_describes_version_license_and_endpoints()
    {
        var meta = ApiMeta.Describe();

        Assert.Equal("v1", meta.ApiVersion);
        Assert.Equal(ApiConstants.SchemaVersion, meta.SchemaVersion);
        Assert.Equal(ApiConstants.License, meta.License);
        Assert.Contains(meta.Endpoints, e => e.Path == "/api/v1/persons/{id}");
        Assert.Contains(meta.Endpoints, e => e.Path == "/api/v1/export");
        Assert.All(meta.Endpoints, e => Assert.StartsWith("/api/v1", e.Path));
    }

    private static DharmatlasDbContext NewDb() =>
        new(new DbContextOptionsBuilder<DharmatlasDbContext>()
            .UseInMemoryDatabase("api-contract-" + Guid.NewGuid())
            .Options);
}
