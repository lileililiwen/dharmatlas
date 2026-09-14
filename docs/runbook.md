# Runbook

Join log, trace, and error report by `X-Correlation-ID` response header.
Traces come from the `Dharmatlas.Host` activity source; the OTLP exporter
endpoint is `DHARMATLAS_OTLP_ENDPOINT` and Sentry reporting is opt-in via
`DHARMATLAS_SENTRY_DSN`. No source text, email, or auth material is recorded.

## Readiness failure

Symptoms: `DharmatlasReadinessFailing` alert, `/health/ready` returns 503.

1. Check `/health/ready` response and host logs filtered by correlation ID.
2. Verify PostgreSQL connectivity and credentials (host fails closed naming
   the missing key; see `docs/operations.md`).
3. If a deploy just rolled out, halt promotion and restore the previous
   signed image (see `docs/operations.md` release train).
4. Restore from backup only; never auto-down-migrate production.

## 5xx spike

Symptoms: `DharmatlasHigh5xxRate` alert.

1. Take one failing correlation ID from the alert or logs.
2. Join structured log, trace (`correlation_id` tag), and scrubbed error
   report by that ID.
3. If scoped to one route, check the recent change and roll back the image
   if the error budget burns faster than the SLO allows.

## Slow queries / slow search

Symptoms: `DharmatlasSlowReads` or `DharmatlasSlowSearch` alert;
`dharmatlas_slow_queries_total` rising.

1. Confirm the p95 panel in `ops/dashboard.md` and the route label.
2. Check PostgreSQL plans for loss of selectivity (see
   `docs/search-fidelity.md` 250 ms adoption gate).
3. Apply index or query-bound fix in staging first; promotion requires
   migration pre-check green.

## Import rejection spike

Symptoms: `DharmatlasImportRejections` rising.

1. Run the importer with `--dry-run` for the coverage/rejection report.
2. Fix manifest tone/tier/claim mapping; do not bypass the
   `SeedPublicationReadiness` resolvability gate.

## Export failure

Symptoms: `DharmatlasExportFailures` alert or falling export success ratio.

1. Check export job state and checksum metadata; retry is safe because
   failed jobs clear partial download state before retry.
2. Verify gzip round-trip and checksum, then re-run the affected export.

## Nightly restore drill failure

Symptoms: `DharmatlasRestoreDrillFailing` or the scheduled
`restore-drill.yml` workflow red.

1. Inspect the drill log: backup, disposable restore, migrate,
   `/health/ready`, entity, and export-checksum steps.
2. Block promotion until the drill is green; the drill database is
   disposable and never production.
