# HANDOFF.md

## Current state

The `project-foundation` change is complete, archived, and promoted to the
project spec baseline (`openspec/specs/project-foundation/spec.md`). The source-first
domain contract is written in `docs/domain-contract.md`, covering the entity and
relationship vocabulary, uncertainty-aware date model, historical-certainty
values, source-linking requirements, and the validation/one-change workflow.

Seven implementation-ready OpenSpec changes remain unimplemented and unarchived
(historical-data-model, timeline-exploration, historical-map,
entity-discovery-and-search, contribution-review-and-revisions,
open-api-and-data-export, ai-assisted-curation).

## Next change

`historical-data-model` is the next active change in the ROADMAP queue. It builds
on the `project-foundation` contract to define the concrete data model
(entities, names, relationships, events, sources, claims, revisions,
contributors) for the 500 BCE–1000 CE MVP. Select it with:

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

- `project-foundation` validation: `openspec validate --all --strict --no-interactive`
  -> **8 passed, 0 failed** (project-foundation plus the seven pending changes).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `project-foundation` archived as `2026-09-07-project-foundation`; its spec
  promoted to `openspec/specs/project-foundation/spec.md` as the acceptance baseline.
- No runtime tests for this documentation/contract change; runtime tests begin
  with `historical-data-model`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
