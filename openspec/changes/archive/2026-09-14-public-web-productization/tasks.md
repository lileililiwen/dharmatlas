# Tasks

- [x] Add client router with API `detailRoute` parity and sourced 404.
- [x] Add per-entity meta/OG/canonical + sitemap without leaking drafts.
- [x] Add i18n catalog (en + stub zh/ja) with fallback-never-blank guarantee.
- [x] Add PWA service worker for shell + visited reads with bounded TTL.
- [x] Add onboarding card + year tour and anonymous PII-free telemetry with schema test.
- [x] Verify deep-link reload, Lighthouse SEO/a11y, airplane-mode revisit, strict validation.

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
