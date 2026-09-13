# Tasks

- [x] Draft `docs/editorial-handbook.md` with tiers, tone, dispute, date-style, and license rules.
- [x] Author `data/seed/v2/manifest.json` (50–100 real records, 7 regions × 6 types, competing claims separate).
- [x] Extend validator/importer with tier check, resolvability gate, tone lint, coverage report, `--dry-run`, transactional abort.
- [x] Add dataset version/changelog/DOI-stub and export reproducibility check.
- [x] Add AI citation-check report (missing-source and tier-mismatch flags, review-only).
- [x] Verify double-import idempotence + export checksum + API evidence spot-checks on Postgres.
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
