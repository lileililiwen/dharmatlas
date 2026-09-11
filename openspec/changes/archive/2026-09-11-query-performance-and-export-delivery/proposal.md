# Proposal: Query Performance and Export Delivery

## What Changes

- Push public query filters, projections, ordering, cursors, and hard limits into the database query path.
- Add supporting PostgreSQL indexes and a measurable threshold for future search-engine adoption.
- Add checksummed export metadata, retry-safe job state, and a gzip streaming delivery path.

## Why

Search, timeline, map, and API list services materialize whole tables before filtering or pagination. Bulk export also builds one unbounded in-memory response, which will fail as the corpus grows.

## Scope

Push filters/order/pagination into PostgreSQL, establish measured indexes and search strategy, bound map/timeline queries, and deliver exports as versioned compressed snapshots with checksums and job/status/download semantics.

## Non-goals

- No premature Elasticsearch deployment.
- No graph database migration.
- No removal of source-first filters.

## Dependencies

Depends on `runtime-host-and-deployment`, `entity-name-persistence-and-read-model`, and `public-read-api-completion`.

## Success criteria

Query memory is bounded by page/viewport limits, explain plans use intended indexes, and large exports can complete without holding the entire serialized document in one request.
