# HANDOFF.md

## Current state

Nine OpenSpec changes are complete and archived:

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

The original implementation queue is archived. A new maturity remediation queue
was authored as nine changes:

1. `runtime-host-and-deployment`
2. `curated-seed-data-and-import`
3. `entity-name-persistence-and-read-model`
4. `provenance-claims-and-evidence`
5. `public-read-api-completion`
6. `public-atlas-web-experience`
7. `authenticated-contribution-governance`
8. `query-performance-and-export-delivery`
9. `observability-and-release-quality`

They cover the gaps identified in the product maturity audit. The first three
changes have now been implemented and archived; six remain as planning
artifacts.

`entity-name-persistence-and-read-model` was archived as
`2026-09-11-entity-name-persistence-and-read-model`. It adds shared entity-name
validation and deterministic canonical/alias projection rules, EF save-boundary
validation, a PostgreSQL primary-name uniqueness migration, and reuse across
search, detail, API, export, and seed validation paths.

- Focused name/persistence tests: **9 passed, 0 failed**.
- Full test suite: **135 passed, 0 failed**.
- PostgreSQL migration `20260911090654_EntityNameInvariants` applied.
- PostgreSQL index verified as unique on `(entity_id, lower(language))` for primary names.
- Transactional PostgreSQL round trip verified for English and Sanskrit aliases.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

## Next change

The next implementation change is `provenance-claims-and-evidence`. Select it with:

```bash
openspec list
```

## Completed change evidence

`runtime-host-and-deployment` was archived as
`2026-09-11-runtime-host-and-deployment`. It adds the executable host,
PostgreSQL migration, health endpoints, Docker Compose stack, CI workflow,
operations guide, and host smoke tests. Local Compose uses host PostgreSQL port
`55434` and host HTTP port `18080` because ports `5432` and `8080` are occupied
by other services; container-internal ports remain `5432` and `8080`.

- Focused host smoke tests: **3 passed, 0 failed**.
- Full test suite: **126 passed, 0 failed**.
- PostgreSQL migration history contains `20260911083028_InitialCreate`.
- Required PostgreSQL tables `entities`, `relationships`, and `sources` verified.
- `/health/live` -> HTTP 200.
- `/health/ready` -> HTTP 200 with PostgreSQL running.
- `/api/v1/meta` -> HTTP 200.
- Docker image build and `docker compose config --quiet` passed.
- `dotnet build Dharmatlas.slnx --no-restore --nologo -m:1` passed with 0 warnings and 0 errors.
- `openspec validate --all --strict --no-interactive` -> **17 passed, 0 failed**.
- `git diff --check` passed.

`curated-seed-data-and-import` was archived as
`2026-09-11-curated-seed-data-and-import`. It adds the versioned seed manifest,
representative source-linked corpus, pure validation, transactional/idempotent
importer, import CLI, and editorial operations guidance.

- Seed corpus: **14 entities, 10 names, 3 sources, 3 claims, 3 relationships**.
- Coverage: all six entity types and all seven MVP regions.
- Focused seed-import tests: **5 passed, 0 failed**.
- Full test suite after import implementation: **131 passed, 0 failed**.
- PostgreSQL CLI import succeeded twice with identical counts; the second run
  did not duplicate records.
- Invalid-reference and no-partial-write tests passed.
- `dotnet build Dharmatlas.slnx --no-restore --nologo -m:1` passed with 0 warnings and 0 errors.
- `openspec validate --all --strict --no-interactive` -> **16 passed, 0 failed**.
- `git diff --check` passed.

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

## Planning evidence

- The nine active changes each contain `proposal.md`, `design.md`, `tasks.md`,
  and `specs/<capability>/spec.md`.
- The dependency sequence is recorded in `ROADMAP.md`; implementation must
  continue through the one-change workflow below.

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
