# Dharmatlas

> An open historical atlas of Buddhism.

Dharmatlas is a public, open-source knowledge infrastructure project mapping the people, places, texts, institutions, traditions, and events that shaped Buddhism across more than two millennia.

It is not a Buddhist encyclopedia, sectarian authority, social network, or source of religious advice. Its core promise is to show where something existed, when it happened, how it spread, and what it was connected to.

## MVP

The first release covers approximately 500 BCE–1000 CE across India, Central Asia, China, Sri Lanka, Tibet, Korea, and Japan. The initial product surface is intentionally small:

- Timeline
- Historical map
- Person, place, and text pages
- Search across entities
- Source records and historical certainty

The initial target is curated, source-linked data rather than a large volume of articles. The repository currently includes a representative reviewed seed manifest; its exact counts are recorded in `HANDOFF.md` and should not be confused with the future coverage target.

## Principles

- Public benefit: no paywall, advertising-led model, or conversion funnel.
- Non-sectarian presentation: distinguish traditional accounts, academic interpretations, and disputed claims.
- Source first: important historical claims should cite primary, archaeological, or scholarly sources.
- Uncertainty is data: support documented, probable, traditional account, disputed, and unknown states, including approximate dates.
- Human review: AI may extract, normalize, translate, summarize, or flag conflicts, but cannot invent citations or publish historical claims automatically.
- Open continuity: design for exportable data so the knowledge can survive the project.

## Stack

ASP.NET Core, PostgreSQL, React/Vite, and provider-neutral PostgreSQL-backed queries. The public UI is served by the ASP.NET host. MapLibre, D3, Cytoscape.js, and Elasticsearch remain deferred until separately justified by an approved change and real usage evidence.

## Status

The product is implemented and verified incrementally through the OpenSpec workflow:

- Source-first domain and EF Core persistence layers, with PostgreSQL migrations.
- Curated seed data and an idempotent import CLI.
- Timeline, map, multilingual search, claims/evidence, and entity read paths.
- A versioned read-only public API (`/api/v1`) with bounded pagination, cache,
  rate-limit behavior, and checksummed gzip export delivery.
- Authenticated contribution review with immutable revisions, optimistic
  concurrency, and provider-neutral subject mapping.
- AI-assisted curation that produces immutable, provenanced drafts under a human
  publication gate.
- A React/Vite public atlas with accessible list fallbacks, plus host health,
  correlation IDs, structured logs, metrics, CI gates, and operational runbooks.

All fifteen OpenSpec changes are archived. There are no active OpenSpec changes;
future work should begin with a new audited proposal. See [ROADMAP.md](ROADMAP.md)
for product direction and [HANDOFF.md](HANDOFF.md) for verified current state.

## License and governance

- Code in this repository is licensed under the MIT License — see `LICENSE`.
- Seed and curated data are released under CC-BY-4.0, recorded in
  `data/seed/v2/manifest.json` (`metadata.license`).
- Contributions follow `CONTRIBUTING.md` (OpenSpec one-change workflow, DCO
  sign-off, seed licensing rules), `CODE_OF_CONDUCT.md`, and `SECURITY.md`.

## Scope boundary

The MVP does not include online temples, community features, live streaming, courses, donations, fortune telling, a Q&A bot, a full Buddhist canon reader, sectarian forums, temple reviews, or social features. Modern Buddhism (1800–present) and the 1000–1800 period follow after the MVP.

## Development

This repository uses OpenSpec for changes. Read [AGENTS.md](AGENTS.md) before working. The current validation command is:

```bash
openspec validate --all --strict --no-interactive
```
