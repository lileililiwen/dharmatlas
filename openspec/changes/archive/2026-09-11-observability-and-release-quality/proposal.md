# Proposal: Observability and Release Quality

## What Changes

- Add privacy-safe request correlation, structured outcome logs, named metrics, and health observability.
- Expand CI with PostgreSQL, migration, host, frontend, and release smoke gates.
- Document backup/restore drills, rollback controls, retention, and incident recovery.

## Why

Current verification is mostly pure unit tests and EF InMemory model checks. There is no evidence that PostgreSQL, HTTP routing, browser behavior, accessibility, or production failures are observable.

## Scope

Add structured logs, correlation IDs, metrics, error reporting hooks, database/API/browser integration gates, accessibility checks, release smoke tests, and operational dashboards/runbooks while keeping provider-neutral application contracts.

## Non-goals

- No mandatory commercial monitoring vendor.
- No collection of unnecessary visitor personal data.
- No relaxing source or review gates to improve deployment velocity.

## Dependencies

Runs after runtime, API, seed, provenance, and web changes; it may add reusable test infrastructure earlier if isolated.

## Success criteria

Operators can detect failed readiness, slow queries, rejected imports, API errors, and broken public journeys; every release has evidence from the relevant real infrastructure.
