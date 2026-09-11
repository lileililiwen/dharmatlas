# Design: Observability and Release Quality

## Signals

Emit structured events for request outcome, query duration, import validation, moderation decisions, export jobs, and AI draft lifecycle. Include correlation IDs and stable entity/submission IDs but exclude source text, email, and private payloads from normal logs.

## Quality gates

CI runs unit tests, PostgreSQL integration/migration tests, host smoke tests, frontend/browser tests, accessibility checks, export checksums, and strict OpenSpec validation. Failures distinguish product defects from unavailable credentials, Docker, or external tile services.

## Operations

Provide dashboards/alerts or vendor-neutral metric names, retention guidance, incident runbook, rollback steps, and a release checklist. Test backups and restore at least in a repeatable non-production environment.

## Verification

Inject database/API/import/export failures and assert logs, metrics, safe responses, retries, and runbook-documented recovery.
