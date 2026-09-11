# Proposal: Entity Name Persistence and Read Model

## Why

`Entity.Names` is ignored by EF Core while `AddName()` mutates only an in-memory list. Aliases can therefore disappear after save, breaking multilingual discovery and canonical identity.

## Scope

Define one persistence path for `EntityName`, enforce primary-name uniqueness at the application and database boundaries, and create shared read-model assembly rules for names, regions, certainty, sources, and related entities.

## What Changes

- Add shared entity-name validation, normalization, canonical-name fallback, and deterministic alias ordering.
- Enforce name validation at EF save boundaries and add a PostgreSQL uniqueness constraint for primary names.
- Reuse the shared projection in search, detail, API, and export paths.

## Non-goals

- No search-engine replacement.
- No automatic transliteration or historical normalization.
- No changes to entity-owned business semantics.

## Dependencies

Uses `Dharmatlas.Domain`, `Dharmatlas.Persistence`, and existing Search/API assemblers. It precedes seed import and public API completion.

## Success criteria

Names added through supported write/import flows survive a real PostgreSQL round trip, canonical names are deterministic, and all read paths return the same aliases.
