# HANDOFF.md

## Current state

Five OpenSpec changes are complete and archived:

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
- `entity-discovery-and-search` — multilingual entity search and entity detail read
  path (`src/Dharmatlas.Search`): `SearchQuery`/`SearchResult`/`EntitySearchHit`
  and `EntityDetail`/`RelatedEntity`/`SourceView` contracts, pure `SearchEngine`
  (canonical/alternate-script/romanization matching with ranking, one stable
  identity per entity, type and region filters, provenance), pure
  `EntityDetailAssembler` (relationships, sources, type-grouped relations, missing
  fields omitted), `SearchQueryService` and `EntityDetailService` over
  `DharmatlasDbContext`. Added PostgreSQL indexes on `EntityName` `Script` and
  `Romanization`. Promoted to `openspec/specs/entity-discovery-and-search/spec.md`.

Three implementation-ready OpenSpec changes remain unimplemented and unarchived
(contribution-review-and-revisions, open-api-and-data-export, ai-assisted-curation).

## Next change

`contribution-review-and-revisions` is the next active change in the ROADMAP queue
(item 5). It adds contribution submission, review, and revision history on top of
the completed read paths. Select it with:

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

- `entity-discovery-and-search` focused tests: **82 passed, 0 failed** (xUnit,
  cumulative suite including prior changes). New coverage: multilingual alias/diacritic
  matching, one-stable-identity-per-entity, ranking (exact primary > exact alias >
  substring), type and region filters, institution region inheritance, ambiguous
  names distinguished by type, empty-term handling, limit, and entity detail
  (relationship grouping, missing-field omission, source visibility).
- `openspec validate --all --strict --no-interactive` -> **8 passed, 0 failed**
  (project-foundation + historical-data-model + timeline-exploration +
  historical-map + entity-discovery-and-search specs plus the three pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `entity-discovery-and-search` archived as `2026-09-07-entity-discovery-and-search`;
  its spec promoted to `openspec/specs/entity-discovery-and-search/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
