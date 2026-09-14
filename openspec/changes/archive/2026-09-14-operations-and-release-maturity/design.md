# Design: Operations and Release Maturity

## Telemetry

OpenTelemetry SDK: ASP.NET instrumentation, trace ID joins `X-Correlation-ID`, OTLP exporter (endpoint by env). Sentry (or hook interface) captures unhandled exceptions with scrub (`email`, `claim text`, `auth claims` denied). Keep existing low-cardinality counters; add `http_server_duration` histogram and export/import/moderation counters to OTel.

## SLOs and alerts

SLOs: public read p95 <300ms, search p95 <250ms (ties to engine gate), readiness success >99.9%, export success >99%. Alert rules with runbook URLs (readiness, 5xx rate, slow-query rate, export failure, restore-drill failure). Dashboard JSON or vendor-neutral PromQL documented.

## Release train

`staging` → `prod` promotion: signed images (cosign) + SBOM (syft) attestations, migration pre-check job, previous-image retention, no auto-down-migrate. K8s minimal manifests or prod compose with resource limits, non-root, secret refs.

## Drills and gates

Nightly scheduled workflow: backup → disposable restore → migrate → `/health/ready` + entity + export-checksum assertions. PR gates: Playwright journeys, axe (0 critical), k6 thresholds (read/search/export). Cost note documents Postgres + tile + telemetry retention spend.

## Verification

Trace-propagation test, scrub test (PII never in span/log), injected 5xx fires alert rule in dry-run, restore drill green, signed SBOM present on release artifact.
