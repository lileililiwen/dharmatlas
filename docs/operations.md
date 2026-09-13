# Operations

## Local startup

The local stack uses PostgreSQL and the executable host. It uses only local
development credentials and must not be used as production configuration.

```bash
docker compose up --build
```

The host is then available at `http://localhost:18080`. Liveness is available at
`/health/live`, readiness at `/health/ready`, and the public API metadata at
`/api/v1/meta`.

## Migrations

Schema changes are versioned with EF Core migrations. Applying migrations is an
explicit operation. For a configured database:

```bash
DHARMATLAS_DATABASE_CONNECTION='Host=localhost;Port=55434;Database=dharmatlas;Username=dharmatlas;Password=...' \
  dotnet ef database update \
  --project src/Dharmatlas.Persistence/Dharmatlas.Persistence.csproj \
  --startup-project src/Dharmatlas.Host/Dharmatlas.Host.csproj
```

The container example enables migration application with
`Dharmatlas__ApplyMigrations=true` only for the local stack. Do not enable this
on production deployments without an operator-controlled rollout procedure.

## Configuration

Set either `ConnectionStrings:Dharmatlas` or
`DHARMATLAS_DATABASE_CONNECTION`. The host fails at startup when neither is
configured. Never commit credentials, connection strings, or database dumps.

### Authentication

- Default (`DHARMATLAS_AUTH_MODE` unset or `unconfigured`): fail-closed.
  Anonymous writes are denied without creating state.
- Production (`DHARMATLAS_AUTH_MODE=oidc`): set `DHARMATLAS_OIDC_ISSUER`,
  `DHARMATLAS_OIDC_AUDIENCE`, and `DHARMATLAS_OIDC_JWKS_URI`. The reference
  handler validates `iss`/`aud`/`exp`, verifies the RS256 signature against
  the provider JWKS (refreshed hourly, faster on unknown `kid` rotation), and
  maps the stable OIDC `sub` to the contributor record with the `roles` claim
  (configurable via `DHARMATLAS_OIDC_ROLE_CLAIM`) mapped to
  contributor/reviewer. Startup names any missing key and refuses to boot.
- Local development only (`DHARMATLAS_AUTH_MODE=dev-loopback` with
  `ASPNETCORE_ENVIRONMENT=Development`): loopback identities via
  `X-Dev-Subject`/`X-Dev-Role` headers. Never enable outside Development;
  startup refuses any other combination.

### Rate limiting

Tiers are configured with `Dharmatlas:RateLimit:Mode` (`Memory` default,
`Postgres` for shared budgets across instances), plus per-minute budgets
`SearchPerMinute` (default 60), `WritePerMinute` (default 30), and
`ExportPerMinute` (default 10). `Postgres` mode requires the database
connection and creates a `rate_limit_hits` timestamp table
(`IF NOT EXISTS`) holding no contributor or content data. Denied requests
receive 429 with `Retry-After: 60`; denied exports serve no partial body.

### Security headers and CORS

The host emits `Content-Security-Policy` (same-origin scripts, styles,
images, connections), `X-Content-Type-Options`, `X-Frame-Options`,
`Referrer-Policy`, `Permissions-Policy`, and `Strict-Transport-Security`
(HTTPS responses only, so local HTTP development is not pinned). CORS
read-access for additional origins is allowlisted only via
`Dharmatlas:Cors:AllowedOrigins` or `DHARMATLAS_CORS_ORIGINS`; the default is
same-origin. The production image runs as non-root (`USER app`).

## Key rotation

JWKS keys rotate provider-side without restart: the cached document refreshes
hourly and immediately on an unknown `kid`. When rotating the provider signing
key, publish the new JWKS entry before signing with it and keep the old entry
until the refresh interval has passed. To rotate database credentials, update
the environment-provided connection string and restart; the host never falls
back to a default credential.

## Readiness and recovery

Liveness confirms that the process is running and does not require PostgreSQL.
Readiness checks PostgreSQL connectivity and should be used by a load balancer.
Before a migration, take a database backup. If a migration fails, stop the
rollout, restore the previous application version, and follow the database
provider's tested rollback/restore procedure; migrations are not automatically
down-migrated.

## Observability

Every request receives an `X-Correlation-ID` response header. If a valid
`X-Correlation-ID` request header is supplied, it is preserved; otherwise the
host generates one. Structured request logs include only method, route, status,
duration, and correlation ID. They must not include source text, contributor
email, authentication claims, or private payloads.

The `/metrics` endpoint exposes vendor-neutral counters. The stable metric names
are `dharmatlas_http_requests_total`, `dharmatlas_http_request_duration_ms`,
`dharmatlas_readiness_failures_total`, `dharmatlas_slow_queries_total`,
`dharmatlas_import_rejections_total`, `dharmatlas_moderation_decisions_total`,
`dharmatlas_ai_drafts_total`, and `dharmatlas_export_jobs_total`. Scrape or
forward these metrics using the deployment's approved monitoring system, with
short retention for request metrics and longer retention for aggregate release
and failure counters.

## Backup, restore, and rollback verification

Use a disposable PostgreSQL instance for a repeatable restore drill. Create a
logical backup, restore it into an empty database, apply the current migrations,
and verify `/health/ready`, `/api/v1/meta`, and one published entity response:

```bash
pg_dump --format=custom "$DHARMATLAS_DATABASE_CONNECTION" --file=/tmp/dharmatlas.verify.dump
createdb dharmatlas_restore_verify
pg_restore --exit-on-error --dbname=dharmatlas_restore_verify /tmp/dharmatlas.verify.dump
```

Keep the previous application image available during rollout. If readiness,
migration, import, export, or public journey checks fail, stop promotion and
restore the previous image. Do not automatically down-migrate production; use
the reviewed migration's `Down` path only in the disposable verification
database, then repeat the restore drill before resuming deployment.

## Release checklist

- Run the .NET suite, PostgreSQL migration gate, web tests/build, browser smoke
  journey, accessibility review, export checksum test, and strict OpenSpec
  validation.
- Confirm readiness and `/metrics` after deployment; retain the release commit,
  migration ID, test output, and backup/restore drill result.
- Confirm source attribution and uncertainty labels remain present in public
  responses after the migration.

## CI

CI restores and builds the solution, runs tests against the repository test
project, applies migrations to a disposable PostgreSQL service, runs frontend
tests and a production build, validates all OpenSpec artifacts, and checks
whitespace. Browser smoke and accessibility review remain release-checklist
gates for the deployed public journey.

## Seed import

After applying migrations, load the reviewed seed snapshot with the import CLI:

```bash
DHARMATLAS_DATABASE_CONNECTION='Host=localhost;Port=55434;Database=dharmatlas;Username=dharmatlas;Password=dharmatlas-local-only' \
  dotnet run --project src/Dharmatlas.Import/Dharmatlas.Import.csproj -- \
  --file data/seed/v1/manifest.json
```

The importer validates the complete manifest before writing and replaces only
records carrying IDs from that manifest. It does not delete unrelated records.
Review source licensing and attribution metadata before adding records.

## Claims and evidence

Every published claim must cite one or more inspectable `Source` records. Use
`SourceLocator` for a page, folio, chapter, inscription entry, or stable URL;
do not replace a locator with an uncited free-text bibliography. A claim with
`TraditionalAccount` interpretation or certainty must retain that label in
editorial and public views. Scholarly interpretations and disagreements are
added as separate claims, with their own sources and certainty, rather than
overwriting an earlier claim.

Only `Published` claims are included in public entity responses and exports.
Draft, rejected, and private claims remain moderation data. Before publication,
editors must confirm that every source reference exists, the source may be
redistributed under its recorded license/attribution terms, and the statement
does not present a traditional account as documented historical fact.
