# Design: Runtime Host and Deployment Foundation

## Components

- `src/Dharmatlas.Host`: executable ASP.NET Core project and `Program.cs`.
- `src/Dharmatlas.Persistence`: PostgreSQL provider registration, migrations, and migration-safe startup helpers.
- `docker-compose.yml` or equivalent local stack: PostgreSQL plus host only.
- `.github/workflows/ci.yml`: restore, build, test, strict OpenSpec validation, and host smoke test.
- `docs/operations.md`: configuration, migration, backup, health, and local startup instructions.

## Runtime flow

The host reads a required database connection setting, registers one scoped `DharmatlasDbContext`, registers timeline/map/search/API services, maps health endpoints and `MapPublicApi()`, and does not call `EnsureCreated`. Migration execution is an explicit command or controlled startup mode.

## Safety

Missing configuration fails with an actionable startup error. Development defaults are local-only and never contain production credentials. Readiness reports database connectivity separately from process liveness.

## Verification

Test service registration and route mapping with an in-process host, run migrations against PostgreSQL in CI, and execute a health plus `/api/v1/meta` smoke test.
