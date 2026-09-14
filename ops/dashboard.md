# Dashboard PromQL

Vendor-neutral panels for the deployment's approved monitoring system.
All queries join to traces and logs via the `correlation_id` / `X-Correlation-ID`.

## Request overview

- Rate: `rate(dharmatlas_http_requests_total[5m])`
- Error ratio: `rate(dharmatlas_http_requests_total{status=~"5.."}[5m]) / rate(dharmatlas_http_requests_total[5m])`
- Read p95: `histogram_quantile(0.95, rate(dharmatlas_http_server_duration_seconds_bucket[5m]))`
- Search p95: `histogram_quantile(0.95, rate(dharmatlas_http_server_duration_seconds_bucket{route=~".*search.*"}[5m]))`

## Release and recovery

- Readiness failures: `rate(dharmatlas_readiness_failures_total[5m])`
- Import rejections by reason: `rate(dharmatlas_import_rejections_total[15m])`
- Moderation decisions: `rate(dharmatlas_moderation_decisions_total[15m])`
- AI drafts by kind: `rate(dharmatlas_ai_drafts_total[15m])`
- Export success ratio: `rate(dharmatlas_export_jobs_total{outcome="success"}[30d]) / rate(dharmatlas_export_jobs_total[30d])`
- Restore drill: `dharmatlas_restore_drill_success` (1 = green, reported in the
  nightly drill workflow log and forwarded by the deployment)

See `docs/slo.md` for thresholds and `docs/runbook.md` for response steps.
