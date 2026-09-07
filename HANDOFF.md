# HANDOFF.md

## Current state

Eight OpenSpec changes are complete and archived:

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
- `open-api-and-data-export` — versioned read-only public API and reproducible
  bulk export (`src/Dharmatlas.Api`): versioned `/api/v1` endpoints for persons,
  events, places, texts, relationships, sources, export, and meta; bounded cursor
  pagination, cache-control, and per-key rate limiting; pure engine
  (`ApiQueryService`, `BulkExporter`, `Paginator`, `RateLimiter`, `ApiMeta`)
  with xUnit contract tests over an in-memory `DbContext`. The published-data
  boundary is enforced structurally — only `Entities`, `EntityNames`,
  `Relationships`, and `Sources` are read, so drafts, rejected contributions, and
  private contributor data never surface. Promoted to
  `openspec/specs/open-api-and-data-export/spec.md`.
- `ai-assisted-curation` — auditable AI-assistant curation (`src/Dharmatlas.AI`):
  a pure, deterministic `AiJobs` engine (entity extraction, name normalization,
  duplicate detection, date-conflict detection) that operates only on supplied
  material, plus an `AiDraft` domain entity recording immutable provenance
  (input reference/text, model, model version, prompt ref, suggestion, confidence,
  timestamp) and an immutable human decision trail. `AiCurationService` records
  accept/reject/return-for-correction decisions and promotes accepted drafts into
  the existing contribution-review queue (publish-through-review); duplicate and
  date-conflict suggestions are review-only and never merge records. Added minimal
  `SubmissionType.Person` so AI entity-extraction drafts can publish through the
  human gate. Promoted to `openspec/specs/ai-assisted-curation/spec.md`.

No implementation-ready OpenSpec changes remain unarchived. The ROADMAP queue
(items 1-7) is exhausted; any further work requires a new proposal.

## Next change

None pending. The ROADMAP queue is empty after `ai-assisted-curation`. To start
new work, open a new OpenSpec change (e.g. `openspec new <change-name>`) and
select it with:

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
  + open-api-and-data-export + ai-assisted-curation specs).
- `git diff --check` and `git status --short`: clean within the committed scope.
- `open-api-and-data-export` focused tests: **111 passed, 0 failed** (xUnit, full
  cumulative suite). New coverage: versioned endpoint shapes (person/event/place/
  text), unknown-id nulls, list filters (year-range overlap, min-certainty, type),
  cursor pagination + limit clamping, published-data boundary (pending and rejected
  submissions excluded), export schema/license/reproducibility, fixed-window rate
  limiter, `ApiException` -> `ProblemDetails`, and `/meta` description.
- `open-api-and-data-export` archived as `2026-09-07-open-api-and-data-export`; its
  spec promoted to `openspec/specs/open-api-and-data-export/spec.md`.
- `ai-assisted-curation` focused tests: **12 passed, 0 failed** (xUnit; full
  cumulative suite **123 passed, 0 failed**). New coverage: full AI-draft provenance
  retention, no automatic publication (acceptance only creates a pending submission),
  citation-verify rejection creates no published claim, non-destructive duplicate and
  date-conflict suggestions (both records preserved, neither date altered),
  publish-through-review (accepted draft -> pending submission -> approved person),
  editability after return-for-correction, immutability of decided drafts, and the
  pure engine's extraction/normalization/duplicate/date-conflict behaviors.
- `ai-assisted-curation` archived as `2026-09-07-ai-assisted-curation`; its spec
  promoted to `openspec/specs/ai-assisted-curation/spec.md`.

## Blocker reporting

Do not claim a change complete when implementation, tests, validation, or archive is incomplete. Record the exact failed command and output summary here, then state the next action that can unblock the work.
