# Public API examples

The public API is versioned under `/api/v1`. Responses use stable string IDs,
retain uncertainty labels and authored date expressions, and contain only
published records.

## Search an alias

```http
GET /api/v1/search?q=Xuanzang&type=Person&limit=20
```

Each hit includes the matched name form, canonical name, certainty, and a
`detailRoute` such as `/persons/{id}`. Query terms may use alternate scripts or
romanizations. `matchKind` explains the match (`Exact`, `Transliteration`,
`Substring`, or `Fuzzy`); `matchedName` is the exact stored form that matched,
so transliteration and fuzzy hits stay transparent. Example hit:

```json
{
  "id": "{xuanzang-id}",
  "type": "Person",
  "canonicalName": "Xuanzang",
  "matchedName": "Hsüan-tsang",
  "matchedForm": "Romanization",
  "matchKind": "Transliteration",
  "score": 60,
  "detailRoute": "/persons/{xuanzang-id}"
}
```

## Explore uncertain dates

```http
GET /api/v1/timeline?fromYear=-250&toYear=100&region=India&limit=50
```

`DisplayDate` is the authored expression. `Certainty` is never inferred from a
numeric bound and must be shown alongside it.

## Bound a map request

```http
GET /api/v1/map?year=650&minLatitude=5&maxLatitude=40&minLongitude=65&maxLongitude=105
```

Map years are bounded to `-5000..3000`; latitude and longitude bounds are
validated before querying. `includeUnknownActivity=true` explicitly opts into
features without normalized activity bounds.

List endpoints use stable entity-id cursors and database-side filters. Responses
are capped at 100 items; map responses are capped at 500 features. The bulk
snapshot includes a dataset revision and SHA-256 checksum. Add `?format=gzip` to
`/api/v1/export` to stream a compressed JSON snapshot directly to the client.

Future search-engine adoption is justified only after PostgreSQL-backed benchmark
fixtures show sustained p95 search latency above 250 ms at the maximum page size,
or query plans show the name indexes are no longer selective for the published
corpus. Until then, the database query remains the source of truth.

## Inspect evidence

```http
GET /api/v1/claims?subjectId={entity-id}
GET /api/v1/claims/{claim-id}
```

Claim responses include statement, certainty, interpretation, source locator,
and inspectable source records. Draft, rejected, and private claims are never
returned. Traditional accounts and competing interpretations remain separately
labeled records.

## Authenticated contributions

The write surface is separate from public reads. Deployments must provide a
verified authentication handler and map its stable subject claim to a
`contributors.external_subject` record. The host never trusts contributor or
reviewer IDs from request bodies.

- `POST /api/v1/contributions` — contributor role; validates JSON shape, cited
  source existence, and target type before queue insertion.
- `GET /api/v1/contributions/mine` — contributor role; returns only the actor's
  submissions.
- `GET /api/v1/contributions/review-queue` — reviewer role.
- `POST /api/v1/contributions/{id}/review` — reviewer role; requires a decision
  reason, rejects self-approval by default, and records a server correlation ID.

Approval is transactional on relational providers. Concurrency is enforced
server-side: `Submission.Version` is an EF concurrency token, so a stale
concurrent decision fails with 400 ("changed while it was being reviewed")
instead of overwriting history. The review request itself carries no version
token — only decision and reason.
