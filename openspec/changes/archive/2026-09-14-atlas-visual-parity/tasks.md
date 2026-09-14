# Tasks

- [x] Wire MapLibre pane to `/api/v1/map` with tile config, clustering, 500-cap handling, and tile-outage list fallback.
- [x] Build D3 timeline bands (interval/approximate/traditional), zoom, region/category filters, keyboard path.
- [x] Build Cytoscape.js 1-hop (max 2-hop) graph with disputed parallel edges and pagination.
- [x] Add loading/empty/error states, reduced-motion, focus-visible controls, text-fallback parity.
- [x] Add component + contract tests and Playwright journeys plus axe check.
- [x] Verify production container build with tile config and strict OpenSpec validation.

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
