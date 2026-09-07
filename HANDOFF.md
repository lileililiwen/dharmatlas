# HANDOFF.md

## Current state

Two OpenSpec changes are complete and archived:

- `project-foundation` — source-first domain contract (`docs/domain-contract.md`),
  promoted to `openspec/specs/project-foundation/spec.md` as the acceptance baseline.
- `historical-data-model` — the durable data model is implemented: a dependency-free
  domain library (`src/Dharmatlas.Domain`) with entities, multilingual names,
  typed sourceable relationships, sources, source-linked claims, revisions, and
  contributors; uncertainty-aware date and certainty value objects with
  domain-boundary validation; and an EF Core persistence layer
  (`src/Dharmatlas.Persistence`) with TPH entities, normalized date-bound columns
  plus a timeline range index, and indexes for name search, relationship
  endpoints, and claim certainty/status. The spec is promoted to
  `openspec/specs/historical-data-model/spec.md`.

Six implementation-ready OpenSpec changes remain unimplemented and unarchived
(timeline-exploration, historical-map, entity-discovery-and-search,
contribution-review-and-revisions, open-api-and-data-export, ai-assisted-curation).

## Next change

`timeline-exploration` is the next active change in the ROADMAP queue. It builds
on the `historical-data-model` to provide timeline querying and filtering over
the 500 BCE–1000 CE MVP. Select it with:

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

- `historical-data-model` focused tests: **31 passed, 0 failed** (xUnit).
- `openspec validate --all --strict --no-interactive` -> **8 passed, 0 failed**
  (project-foundation + historical-data-model specs plus the six pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `historical-data-model` archived as `2026-09-07-historical-data-model`; its spec
  promoted to `openspec/specs/historical-data-model/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
