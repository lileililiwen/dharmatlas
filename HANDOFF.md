# HANDOFF.md

## Current state

Three OpenSpec changes are complete and archived:

- `project-foundation` — source-first domain contract (`docs/domain-contract.md`),
  promoted to `openspec/specs/project-foundation/spec.md` as the acceptance baseline.
- `historical-data-model` — the durable data model is implemented (dependency-free
  domain library `src/Dharmatlas.Domain`, EF Core persistence `src/Dharmatlas.Persistence`);
  promoted to `openspec/specs/historical-data-model/spec.md`.
- `timeline-exploration` — the read-only timeline read path is implemented
  (`src/Dharmatlas.Timeline`): `TimelineQuery`/`EventSummary` contracts, a pure
  `TimelineEngine` (overlap + category/region filtering, unknown-date opt-in),
  `TimelineQueryService`, zoom presets, keyboard `EventFocusNavigator`, and
  `TimelineViewState`. The `Event` entity gained `Category`, `Region`, and
  `Certainty` to support filtering and certainty display. Promoted to
  `openspec/specs/timeline-exploration/spec.md`.

Five implementation-ready OpenSpec changes remain unimplemented and unarchived
(historical-map, entity-discovery-and-search, contribution-review-and-revisions,
open-api-and-data-export, ai-assisted-curation).

## Next change

`historical-map` is the next active change in the ROADMAP queue. It builds on the
data model and timeline to provide a time-filtered historical map for places,
institutions, routes, and archaeological sites. Select it with:

```bash
openspec list
```

## Exact delivery workflow

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.

## Verification evidence

- `timeline-exploration` focused tests: **51 passed, 0 failed** (xUnit, cumulative
  suite including prior changes).
- `openspec validate --all --strict --no-interactive` -> **8 passed, 0 failed**
  (project-foundation + historical-data-model + timeline-exploration specs plus
  the five pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `timeline-exploration` archived as `2026-09-07-timeline-exploration`; its spec
  promoted to `openspec/specs/timeline-exploration/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
