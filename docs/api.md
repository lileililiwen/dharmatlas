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
romanizations.

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

## Inspect evidence

```http
GET /api/v1/claims?subjectId={entity-id}
GET /api/v1/claims/{claim-id}
```

Claim responses include statement, certainty, interpretation, source locator,
and inspectable source records. Draft, rejected, and private claims are never
returned. Traditional accounts and competing interpretations remain separately
labeled records.
