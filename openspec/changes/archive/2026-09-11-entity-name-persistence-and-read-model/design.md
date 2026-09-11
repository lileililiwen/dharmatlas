# Design: Entity Name Persistence and Read Model

## Persistence rule

`EntityName` is the authoritative persisted record. Domain helpers return validated name records; application services explicitly add/update `EntityName` rows in the same transaction as the entity. EF navigation configuration must not create a second competing name store.

## Invariants

Require non-empty language/script/value, bounded metadata, one primary name per entity/language, and stable ordering by primary flag, language, script, romanization, and ID. Add a database-compatible uniqueness constraint for primary names.

## Shared projection

Centralize canonical-name and alias projection so Search, API, export, and detail pages use identical fallback behavior and never substitute an ID without marking the name as missing.

## Verification

Use PostgreSQL round-trip tests, duplicate-primary race tests where supported, and cross-service projection contract tests.
