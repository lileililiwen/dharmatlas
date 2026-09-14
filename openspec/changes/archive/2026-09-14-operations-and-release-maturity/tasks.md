# Tasks

- [x] Wire OpenTelemetry traces + Sentry hook with PII scrub tests.
- [x] Define SLOs, PromQL/alert rules, and runbook links.
- [x] Add staging/prod promotion with cosign signing + SBOM attestation and migration pre-check.
- [x] Add nightly backup→disposable-restore drill with ready/entity/checksum assertions.
- [x] Add Playwright + axe + k6 gates with thresholds and cost note.
- [x] Run strict OpenSpec validation and `git diff --check`.

## Delivery workflow

1. Select this change with `openspec list`.
2. Implement only this change and its tests.
3. Update this `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.
