# Cost note

Indicative monthly spend drivers for a small single-instance deployment.
Adjust for region, retention, and traffic.

| Driver | Control |
| --- | --- |
| PostgreSQL (managed or self-hosted volume + backups) | Single `db` instance; nightly logical backup retained 30 days; drill uses a disposable database, not a standing replica. |
| Map tiles | Static tile style via `VITE_TILE_URL`; no per-request tile server. List fallback keeps the atlas usable on tile outage, so tile spend stays flat. |
| Telemetry (OTLP/Sentry/metrics) | Short retention for request traces and duration histograms; longer retention only for aggregate release and failure counters. No high-cardinality labels (no query text or user IDs). Sentry transmits scrubbed errors only, opt-in via `DHARMATLAS_SENTRY_DSN`. |
| CI (Playwright/axe/k6, restore drill) | Scheduled drill runs nightly on ephemeral runners with a disposable database; load gate uses a short k6 run against staging, not sustained soak. |

No multi-region active-active cost is in scope.
