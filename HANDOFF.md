# HANDOFF.md

## Current state

Fifteen OpenSpec changes are complete and archived:

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

They cover the gaps identified in the product maturity audit. All nine maturity
remediation changes have now been implemented and archived.

`observability-and-release-quality` was archived as
`2026-09-11-observability-and-release-quality`. It adds privacy-safe structured
request logs, correlation IDs, named metrics, readiness failure counting,
frontend CI gates, host smoke coverage, and operational documentation for
backup/restore, rollback, retention, and incident recovery.

- Full .NET test suite: **148 passed, 0 failed**.
- Frontend tests: **1 passed, 0 failed**; production build passed with Vite **6.4.3**.
- Host smoke coverage verifies correlation-ID propagation and `/metrics` availability.
- Host build: passed with 0 warnings and 0 errors.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

`query-performance-and-export-delivery` was archived as
`2026-09-11-query-performance-and-export-delivery`. It moves public list,
search, timeline, and map filtering into bounded database queries with stable
ID ordering and hard limits; adds PostgreSQL query-support indexes; and adds
checksummed export metadata, retry-safe job state, and a gzip response stream.

- Full .NET test suite: **147 passed, 0 failed**.
- PostgreSQL migration `20260911101010_QueryPerformanceIndexes` applied on host
  port `55434`.
- Export coverage verifies valid gzip round-trip, checksum metadata, and that a
  failed job clears partial download state before retry.
- Search-engine adoption threshold documented at sustained p95 above 250 ms at
  maximum page size or loss of selectivity in PostgreSQL plans.
- Host build: passed with 0 warnings and 0 errors.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

`provenance-claims-and-evidence` was archived as
`2026-09-11-provenance-claims-and-evidence`. It adds claim interpretation and
source-locator metadata, a publication-safe claim boundary, evidence sections
on person/event/place/text API and detail views, and published claims in bulk
exports. Traditional and scholarly interpretations remain explicitly labeled;
conflicting published claims remain separate.

- Focused provenance tests: **3 passed, 0 failed**.
- Full test suite: **138 passed, 0 failed**.
- PostgreSQL migration `20260911092229_ClaimEvidenceMetadata` applied.
- PostgreSQL `claims.interpretation` and `claims.source_locator` columns verified.
- Rejected claims were excluded from API/export projections; competing published claims remained visible.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

`authenticated-contribution-governance` was archived as
`2026-09-11-authenticated-contribution-governance`. It adds provider-neutral
subject-to-contributor mapping, persisted contributor roles, protected
contribution/reviewer routes, source and target reference validation,
server-owned timestamps, self-approval denial, relational approval
transactions, optimistic submission versioning, and correlation IDs on review
decisions and revisions. The default host authentication handler is fail-closed
and trusts no request headers; deployments must replace it with their verified
OIDC, SAML, or gateway handler.

- Focused and full .NET test suite: **145 passed, 0 failed**.
- Coverage includes anonymous route denial, identity/role mapping, source and
  target validation, server timestamp, self-approval, audit correlation, and
  stale concurrent review rejection.
- PostgreSQL migration `20260911095511_AuthenticatedContributionGovernance`
  applied on host port `55434`; identity, role, version, and correlation columns
  verified.
- Host build: passed with 0 warnings and 0 errors.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

`public-atlas-web-experience` was archived as
`2026-09-11-public-atlas-web-experience`. It adds a React/Vite public client
served by the ASP.NET host and included in the production container build. The
first-visit surface includes multilingual search, stable entity navigation,
uncertainty-aware dates and labels, claim/source inspection, timeline and
map/list exploration, loading/empty/error states, responsive keyboard-visible
controls, and reduced-motion support. Map features retain an accessible list
fallback instead of depending on tiles alone.

- Frontend formatter tests: **3 passed, 0 failed**.
- Frontend production build: passed with Vite **6.4.3**.
- Frontend production dependency audit: **0 vulnerabilities**.
- Headless Chrome smoke render confirmed the public landmarks, search label,
  uncertainty guidance, timeline entry point, map/list entry point, and API link.
- Full .NET test suite: passed with no reported failures.
- Production Docker image build: passed as `dharmatlas:web-experience`.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

`public-read-api-completion` was archived as
`2026-09-11-public-read-api-completion`. It adds versioned HTTP routes and
explicit DTOs for multilingual search, bounded timeline/map exploration,
institution and tradition navigation, and published claim/evidence access.
Map viewport and year inputs are validated before querying; search results carry
stable detail links; API metadata and examples are maintained in `docs/api.md`.

- Focused API/host contract tests: **22 passed, 0 failed**.
- Full test suite: **140 passed, 0 failed**.
- New route validation covers required search terms and invalid map years.
- `openspec validate --all --strict --no-interactive` -> **15 passed, 0 failed**.
- `git diff --check` passed.

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

`genuine-historical-corpus` was archived as
`2026-09-13-genuine-historical-corpus`. It replaces the v1 placeholder seed
with a real 500 BCE–1000 CE corpus (`data/seed/v2/manifest.json`:
**53 entities, 75 names, 13 sources, 44 claims, 14 relationships**),
`docs/editorial-handbook.md`, source tiers persisted via migration
`20260913232755_SourceTiers` (nullable `sources.tier`) and surfaced in
detail/API/export views, claim interpretation + source-locator import mapping,
tone/tier validation, `SeedPublicationReadiness` resolvability gate,
`SeedCoverage` region/type matrix, `--dry-run` coverage/rejection report,
review-only `CitationChecker`, dataset version `seed-2026-Q4` with
`data/seed/v2/CHANGELOG.md` and DOI stub. Competing claims (Buddha dates,
Japan 538 vs 552, First Council) persist as separate records.

- New corpus tests: **11 passed, 0 failed**; full .NET suite: **159 passed, 0 failed**.
- PostgreSQL: migration applied; CLI double import idempotent
  (53/75/13/44/14 twice, no duplication); tier/interpretation/locator columns verified.
- API spot-checks: Ashoka (Documented/Primary edict + TraditionalAccount chronicle
  as separate claims), Xuanzang (Probable/Traditional), Nalanda institution sources.
- Operator note: importer upserts by stable ID and never deletes; v1 rows coexist
  in old databases, so production cutover should import v2 into a fresh database.
- `openspec validate --all --strict --no-interactive` -> **21 passed, 0 failed**.
- `git diff --check` passed.

`open-governance-and-production-security` was archived as
`2026-09-13-open-governance-and-production-security`. It makes the repo
adoptable and deployable: `LICENSE` (MIT code; data CC-BY-4.0 in
`data/seed/v2/manifest.json` `metadata.license`), `CONTRIBUTING.md` (OpenSpec
one-change workflow, DCO sign-off, seed licensing), `CODE_OF_CONDUCT.md`,
`SECURITY.md` (7-day acknowledgement, 90-day remediation target), all linked
from README; a dependency-free OIDC JWT-bearer reference handler
(`OidcReferenceHandler` + cached `JwksKeyProvider` with rotation, `sub` to
`NameIdentifier`/`sub` mapping for the existing contributor resolver,
contributor/reviewer role mapping, unsigned/alien tokens rejected); a
dev-only loopback handler gated to `DHARMATLAS_AUTH_MODE=dev-loopback` +
Development (refused otherwise); fail-closed startup naming missing DB/OIDC
keys; CSP/HSTS(HTTPS-only)/CORS-allowlist middleware; non-root container
(`USER app`, verified `whoami` -> `app`); tiered rate limits
(search 60/write 30/export 10 per minute) over `IRateLimitStore` with the
in-memory default plus a Postgres-backed shared-budget option
(`rate_limit_hits` table, no contributor/content data); `Directory.Build.props`
NuGet audit, `npm audit --audit-level=high`, gitleaks secret scan, and
Dependabot (NuGet + npm) in CI with `TreatWarningsAsErrors` (NU1801 exempt
for offline builds).

- New governance tests: **21 passed, 0 failed**; full .NET suite: **180 passed, 0 failed**.
- Host build: passed with 0 warnings and 0 errors, including the
  `TreatWarningsAsErrors` gate (NU1801 exempt).
- Container `dharmatlas:governance` builds; fail-closed verified inside the
  image for missing DB and for `oidc` mode with missing OIDC keys.
- Burst verified over HTTP: third request at SearchPerMinute=2 -> 429 with
  `Retry-After`; denied export -> 429 with empty body (no partial export).
- Deny coverage: anonymous contribution write denied (existing host smoke),
  self-approval denied (existing contribution service test), expired/wrong-
  audience/wrong-issuer/tampered/unsigned OIDC tokens denied (new).
- `openspec validate --all --strict --no-interactive` -> **22 passed, 0 failed**.
- `git diff --check` passed.

`multilingual-search-fidelity` was archived as
`2026-09-13-multilingual-search-fidelity`. It upgrades search to script-aware,
transliteration-tolerant retrieval: pure `NameNormalizer` in
`Dharmatlas.Domain` (NFKC, casefold, diacritic strip with kana-voicing guard,
compatibility folds, Wade-Giles/Pinyin `hs`→`x`/`ts`→`z`/`-ien`→`-ian` key,
CJK bigrams, bounded Levenshtein), ranking pipeline
exact(100/80) > transliteration(70/60) > substring(40/30) > fuzzy(20/15,
distance ≤ 2) with primary-over-alias at every layer, hard cap 100 results
(1000 candidates), additive `MatchKind` on hits and the public search DTO
(`matchedName`/`matchedForm`/`matchKind`), `entity_names.normalized_value`
storage populated on save plus migration with backfill and `pg_trgm` GIN
indexes, normalized+key candidate prefilter in `SearchQueryService`,
`tests/.../fixtures/multilingual.json` (11 entities, 53 cases, 6 scripts ×
4 romanizations, ambiguous multi-hit case), and `docs/search-fidelity.md`
with the 250 ms p95 adoption gate.

- New tests: **56 passed, 0 failed** (normalizer vectors, engine
  transliteration/fuzzy/caps/no-merge, shadow-column, storage-backed
  diacritic/variant/script resolution); full .NET suite: **238 passed, 0 failed**.
- Benchmark: **53/53 pass, p50 0.92 ms, p95 2.46 ms** at limit=100 over
  511 entities (500 distractors); gate p95 ≤ 250 ms.
- PostgreSQL: migration applied; `normalized_value` + btree + both GIN trigram
  indexes verified; CLI double import idempotent (53/75/13/44/14 twice);
  85/85 names populated; EXPLAIN uses the trigram bitmap when selective
  (85-row corpus seq-scans by planner choice — expected, not a regression).
- Live host spot-checks on Postgres: `q=Xuanzang` → 200;
  `q=hsuantsang` → `Transliteration` hits (score 70) on both coexisting v1+v2
  rows; `q=玄奘` → empty because the seed stores latin values under `hani`
  script (corpus gap, engine covered by fixture CJK cases).
- `openspec validate --all --strict --no-interactive` -> **21 passed, 0 failed**.
- `git diff --check` passed.

## Next change

One active maturity follow-up change remains unimplemented (all tasks
unchecked). `openspec list` shows:

- `operations-and-release-maturity` (0/6 tasks)

`public-web-productization` was archived as
`2026-09-14-public-web-productization`. It adds history-API deep links with
API `detailRoute` parity (`web/src/routes.js`) and a sourced 404, per-entity
title/description/canonical/OG via client `meta.js` plus host meta injection
on the index fallback and `/sitemap.xml` over published `Entities` only
(`src/Dharmatlas.Host/Web/ProductizationEndpoints.cs`), an en + stub zh/ja
chrome catalog with English fallback (`web/src/i18n.js`), a service worker
caching the shell plus visited reads with 7-day TTL and a localStorage
visited-read fallback with stale indicator (`web/public/sw.js`,
`web/src/offline.js`), and a dismissible onboarding card with 3-step year
tour plus anonymous PII-free telemetry with schema tests
(`web/src/onboarding.js`, `web/src/telemetry.js`).

- New productization tests: **6 passed, 0 failed** (router parity,
  i18n fallback, telemetry PII rejection, meta, offline TTL, tour);
  full web suite: **14 passed, 0 failed**; production Vite build passed.
- New .NET productization tests: **3 passed, 0 failed** (entity-path
  parity, sitemap published-only, meta injection); full .NET suite:
  **241 passed, 0 failed**.
- Host build: passed with 0 warnings and 0 errors.
- Deep-link reload survives via the host index fallback (replacing
  `MapFallbackToFile`); unknown IDs render the sourced 404 without draft
  leaks; claim text is never auto-translated.
- `openspec validate --all --strict --no-interactive` -> **21 passed, 0 failed**.
- `git diff --check` passed.

`atlas-visual-parity` was archived as
`2026-09-14-atlas-visual-parity`. It wires a MapLibre time-filtered map
pane to `/api/v1/map` (configurable `VITE_TILE_URL` + attribution,
clustering beyond 60 features, 500-cap refinement prompt, unknown-activity
opt-in default off, tile-outage list fallback with notice), a D3 timeline
pane rendering interval dates as bands (approximate dashed + `ca.` label,
traditional `◈` glyph + tooltip, zoom in/out/reset with keyboard arrows,
region/category filters), and a Cytoscape.js relationship graph pane
(1-hop default, max 2-hop bounded to 5 first-hop neighbors, disputed
relations as parallel edges, 25-per-page pagination with show-more). Every
pane ships an identical semantic list fallback; reduced-motion disables
transitions; controls are focus-visible with ARIA roles. Pure helpers
(`geo.js`, `timelineBands.js`, `graph.js`) covered by
`web/src/visualParity.test.js`; Playwright journeys + axe gate in
`web/e2e/atlas.spec.js`; production container takes tile config via
`ARG VITE_TILE_URL` / `VITE_TILE_ATTRIBUTION` with no secrets.

- New visual-parity tests: **5 passed, 0 failed** (cap, clustering,
  band/approximate/traditional, filter parity, disputed-split + pagination);
  full web suite: **8 passed, 0 failed**; production Vite build passed
  (maplibre-gl **6.9.0**, d3 **7.9.0**, cytoscape **3.34.3**).
- Full .NET suite: **238 passed, 0 failed**.
- Frontend audit: **0 vulnerabilities** (upgraded maplibre-gl 4.7.1 ->
  6.9.0 to clear GHSA-jrc7-96c5-q579).
- `openspec validate --all --strict --no-interactive` -> **21 passed, 0 failed**.
- `git diff --check` passed.

## Build order

Implement strictly one change at a time through the exact delivery workflow:

1. `genuine-historical-corpus` — DONE (archived 2026-09-13).
2. `open-governance-and-production-security` — DONE (archived 2026-09-13).
3. `multilingual-search-fidelity` — DONE (archived 2026-09-13).
4. `atlas-visual-parity` next — DONE (archived 2026-09-14).
5. `public-web-productization` — DONE (archived 2026-09-14).
6. `operations-and-release-maturity` next last — OTel traces, Sentry hook, SLOs,
   signed promotion with SBOM, nightly restore drill, Playwright/axe/k6 gates;
   needs the full product surface to observe and gate.

Start with:

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

- The nine maturity changes each contain archived `proposal.md`, `design.md`, `tasks.md`,
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
