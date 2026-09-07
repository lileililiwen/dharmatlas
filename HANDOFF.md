# HANDOFF.md

## Current state

Six OpenSpec changes are complete and archived:

- `project-foundation` — source-first domain contract (`docs/domain-contract.md`),
  promoted to `openspec/specs/project-foundation/spec.md` as the acceptance baseline.
- `historical-data-model` — durable data model (dependency-free domain library
  `src/Dharmatlas.Domain`, EF Core persistence `src/Dharmatlas.Persistence`);
  promoted to `openspec/specs/historical-data-model/spec.md`.
- `timeline-exploration` — read-only timeline read path (`src/Dharmatlas.Timeline`);
  promoted to `openspec/specs/timeline-exploration/spec.md`.
- `historical-map` — read-only time-filtered map read path (`src/Dharmatlas.Map`);
  promoted to `openspec/specs/historical-map/spec.md`.
- `entity-discovery-and-search` — multilingual entity search and entity detail read
  path (`src/Dharmatlas.Search`); promoted to
  `openspec/specs/entity-discovery-and-search/spec.md`.
- `contribution-review-and-revisions` — contribution workflow and audit trail
  (`src/Dharmatlas.Contributions`): `Submission`/`ReviewDecision` contracts and
  `SubmissionStatus`/`SubmissionType`/`ReviewDecisionType` enums in
  `Dharmatlas.Domain`, pure `SubmissionValidator` (supported types + required source
  references) and `ReviewEngine` (approve/request-changes/reject transitions, conflict
  surfacing, immutable field-level `Revision` with contributor, reviewer, reason,
  sources, and changed fields). `ContributionService` routes drafts to pending only
  (no direct publication), applies approved changes to the published record, and
  exposes history, contributor, and reviewer views. `Revision` gained `ReviewerId`,
  `ChangedFieldsJson`, and `SourceIds`; `Submission`/`ReviewDecision` got owned
  persistence. Promoted to
  `openspec/specs/contribution-review-and-revisions/spec.md`.

Two implementation-ready OpenSpec changes remain unimplemented and unarchived
(open-api-and-data-export, ai-assisted-curation).

## Next change

`open-api-and-data-export` is the next active change in the ROADMAP queue (item 6).
It adds a read-only public API and data export on top of the completed read paths.
Select it with:

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

- `contribution-review-and-revisions` focused tests: **95 passed, 0 failed**
  (xUnit, cumulative suite including prior changes). New coverage: submission
  validation (missing sources, malformed payloads), decision transitions
  (approve/request-changes/reject), conflict detection against approved changes,
  field-level diff, immutable revision provenance, no-direct-publication gate
  (draft stays pending until review), approved date correction updating the
  published record, rejected submissions leaving records unchanged, and
  contributor/reviewer queue views.
- `openspec validate --all --strict --no-interactive` -> **8 passed, 0 failed**
  (project-foundation + historical-data-model + timeline-exploration +
  historical-map + entity-discovery-and-search + contribution-review-and-revisions
  specs plus the two pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `contribution-review-and-revisions` archived as
  `2026-09-07-contribution-review-and-revisions`; its spec promoted to
  `openspec/specs/contribution-review-and-revisions/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
