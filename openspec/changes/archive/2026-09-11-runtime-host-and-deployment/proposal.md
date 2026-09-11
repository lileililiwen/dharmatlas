# Proposal: Runtime Host and Deployment Foundation

## Why

Dharmatlas currently contains .NET class libraries but no executable host, database startup, migrations, health endpoint, or reproducible deployment path. The project cannot be run by a contributor or consumed by the public.

## Scope

Add a minimal ASP.NET Core host that wires the existing read services and API, configures PostgreSQL through environment-backed options, applies versioned EF migrations explicitly, exposes health/readiness checks, and provides local Docker and CI instructions.

## What Changes

- Add `Dharmatlas.Host` as the executable composition root.
- Add PostgreSQL migrations, health/readiness endpoints, local Compose configuration, CI gates, and operations documentation.
- Add host smoke tests for liveness, readiness, and public API metadata.

## Non-goals

- No frontend implementation.
- No production cloud vendor selection.
- No automatic destructive schema changes.
- No authentication or contribution endpoints; those belong to `authenticated-contribution-governance`.

## Dependencies

Uses the existing `Dharmatlas.Api`, `Dharmatlas.Persistence`, `Dharmatlas.Search`, `Dharmatlas.Timeline`, and `Dharmatlas.Map` libraries. The host must remain the composition root and must not move domain policy into it.

## Success criteria

A fresh checkout can start a documented local stack, run migrations deliberately, answer health checks, and serve the existing public read API with a smoke test.
