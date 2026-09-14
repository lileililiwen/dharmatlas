# Proposal: Operations and Release Maturity

## What Changes

Close the prototype-to-product operations gap: OpenTelemetry traces + Sentry error reporting, SLOs with alerts, staging/prod promotion with image signing + SBOM, automated backup/restore + migration-rollback drill in CI, Playwright e2e + axe + k6 load gates, and cost/runbook documentation.

## Why

Current observability is counters + text `/metrics`; backup/restore is a manual doc; e2e/a11y/load are checklist items, not gates. Failures in prod would be undetected and unrecoverable with confidence.

## Scope (Boundary)

In scope: OTel SDK wiring (trace propagates `X-Correlation-ID`), Sentry (or vendor-neutral hook) with PII scrub, SLOs (read p95, search p95, readiness, export success), alert rules + runbook links, `staging`/`prod` compose or minimal K8s manifests, cosign + SBOM attestation, nightly restore drill job, Playwright journeys + axe + k6 thresholds, cost note.
Out of scope: multi-region active-active, commercial APM mandate, log-everything (privacy budget stays), automatic production down-migration.

## Use cases

- UC1: A 5xx spike pages with trace + correlation ID linking log, trace, and failing journey.
- UC2: A bad migration fails in staging promotion; prod stays on the signed previous image.
- UC3: Nightly job restores backup into disposable DB and verifies `/health/ready` + one entity + export checksum.

## Non-goals / Exceptions

- No PII in traces/logs/metrics (source text, email, claims stay out); scrub is tested.
- No auto-rollback of data migrations in prod; restore-from-backup is the only blessed path.
- No unbounded cardinality labels (user IDs, query text) in metrics.
- No removal of existing privacy-safe counters; new telemetry is additive.

## Dependencies

Builds on `observability-and-release-quality`, `runtime-host-and-deployment`, `query-performance-and-export-delivery`. Runs after governance/security change for prod secrets.

## Success criteria

Traces sampled end-to-end, alerts fire on injected failure, signed SBOM artifacts produced, restore drill green in CI schedule, e2e/a11y/load gates enforced, runbook covers readiness/import/export/slow-query paths.
