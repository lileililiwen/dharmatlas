# HANDOFF.md

## Current state

Four OpenSpec changes are complete and archived:

- `project-foundation` — source-first domain contract (`docs/domain-contract.md`),
  promoted to `openspec/specs/project-foundation/spec.md` as the acceptance baseline.
- `historical-data-model` — durable data model (dependency-free domain library
  `src/Dharmatlas.Domain`, EF Core persistence `src/Dharmatlas.Persistence`);
  promoted to `openspec/specs/historical-data-model/spec.md`.
- `timeline-exploration` — read-only timeline read path (`src/Dharmatlas.Timeline`);
  promoted to `openspec/specs/timeline-exploration/spec.md`.
- `historical-map` — read-only time-filtered map read path (`src/Dharmatlas.Map`):
  `MapQuery`/`MapFeature` contracts, pure `MapEngine` (activity-interval overlap
  filtering with unknown-activity opt-in), `MapQueryService`, `MapClusterer`,
  `MapSelectionNavigator`, and `MapViewState` list fallback. `Place` gained
  `PlaceKind`/`Region`/`Activity`/`Certainty`/`SourceIds` and `Institution` gained
  `PlaceId`/`Activity`/`Certainty`/`SourceIds`. Promoted to
  `openspec/specs/historical-map/spec.md`.

Four implementation-ready OpenSpec changes remain unimplemented and unarchived
(entity-discovery-and-search, contribution-review-and-revisions,
open-api-and-data-export, ai-assisted-curation).

## Next change

`entity-discovery-and-search` is the next active change in the ROADMAP queue. It
builds on the data model to provide person/place/text discovery and search across
canonical and alternative names. Select it with:

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

- `historical-map` focused tests: **68 passed, 0 failed** (xUnit, cumulative
  suite including prior changes).
- `openspec validate --all --strict --no-interactive` -> **8 passed, 0 failed**
  (project-foundation + historical-data-model + timeline-exploration +
  historical-map specs plus the four pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `historical-map` archived as `2026-09-07-historical-map`; its spec promoted to
  `openspec/specs/historical-map/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
