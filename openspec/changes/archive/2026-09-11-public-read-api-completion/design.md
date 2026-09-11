# Design: Complete Public Read API

## Endpoint groups

Add `/api/v1/search`, `/timeline`, `/map`, `/institutions`, `/traditions`, `/claims`, and evidence-aware entity detail routes. Preserve existing persons/events/places/texts/relationships/sources/export/meta routes and return `ProblemDetails` for invalid filters and IDs.

## Contracts

Use explicit request/response records with schema version metadata, stable IDs, cursor pagination where lists can grow, uncertainty-aware date views, source references, and links between related resources. Map responses include feature type, coordinates/geometry, active interval, certainty, and list fallback metadata.

## Verification

Use ASP.NET in-process integration tests against a test database, route contract snapshots, published-boundary tests, and representative multilingual/uncertain fixtures.
