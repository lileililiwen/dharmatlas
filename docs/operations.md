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
DHARMATLAS_DATABASE_CONNECTION='Host=localhost;Port=5432;Database=dharmatlas;Username=dharmatlas;Password=...' \
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

## Readiness and recovery

Liveness confirms that the process is running and does not require PostgreSQL.
Readiness checks PostgreSQL connectivity and should be used by a load balancer.
Before a migration, take a database backup. If a migration fails, stop the
rollout, restore the previous application version, and follow the database
provider's tested rollback/restore procedure; migrations are not automatically
down-migrated.

## CI

CI restores and builds the solution, runs tests against the repository test
project, applies migrations to a disposable PostgreSQL service, validates all
OpenSpec artifacts, and checks whitespace. Browser and frontend gates will be
added by `public-atlas-web-experience`.

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
