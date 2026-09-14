# SLOs

Release-critical signals with thresholds, PromQL, and runbook links.
Full procedures live in `docs/runbook.md`.

| Signal | SLO (30-day window) | PromQL (indicative) |
| --- | --- | --- |
| Public read latency | p95 < 300 ms | `histogram_quantile(0.95, rate(dharmatlas_http_server_duration_seconds_bucket[5m])) < 0.3` |
| Search latency | p95 < 250 ms | `histogram_quantile(0.95, rate(dharmatlas_http_server_duration_seconds_bucket{route=~".*search.*"}[5m])) < 0.25` |
| Readiness success | > 99.9% | `1 - rate(dharmatlas_readiness_failures_total[5m]) > 0.999` |
| Export success | > 99% | `rate(dharmatlas_export_jobs_total{outcome="success"}[30d]) / rate(dharmatlas_export_jobs_total[30d]) > 0.99` |
| 5xx rate | < 0.1% of requests | `rate(dharmatlas_http_requests_total{status=~"5.."}[5m]) / rate(dharmatlas_http_requests_total[5m]) < 0.001` |

Notes:

- Cardinality stays low: labels are method, route template, status, and
  bounded outcome/reason only. Query text, user IDs, emails, and claim text
  never appear as labels.
- Search p95 ties to the engine gate documented in `docs/search-fidelity.md`:
  sustained p95 above 250 ms at maximum page size triggers search-engine adoption.
- Alert rules are defined in `ops/prometheus-alerts.yml`. Every alert carries
  a `runbook_url` annotation pointing at `docs/runbook.md`.
