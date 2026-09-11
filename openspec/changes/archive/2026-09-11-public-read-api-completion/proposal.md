# Proposal: Complete Public Read API

## Why

Existing timeline, map, search, institution, tradition, and claim services are not wired into HTTP routes. The current public API cannot support the promised atlas navigation.

## Scope

Expose tested versioned endpoints for global search, timeline, map features, all MVP entity types, claims/evidence, source records, related entities, and documented pagination/filter/error behavior. Wire service registration through the runtime host.

## What Changes

- Add versioned HTTP routes for search, timeline, bounded map queries, institutions, traditions, and claims.
- Add explicit DTOs, stable detail links, validation errors, and API documentation examples.
- Extend API metadata so clients can discover the complete public read surface.

## Non-goals

- No write endpoints.
- No authentication in this change.
- No breaking changes to `/api/v1` shapes without a version bump.
- No graph database.

## Dependencies

Depends on `runtime-host-and-deployment`, `entity-name-persistence-and-read-model`, and `provenance-claims-and-evidence`.

## Success criteria

A browser or API client can discover an entity, inspect its timeline/map context and evidence, and navigate through stable links using only documented public endpoints.
