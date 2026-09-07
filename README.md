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

The initial target is curated, source-linked data rather than a large volume of articles. Planned seed scale is 150 people, 300 events, 100 places, 50 institutions, 100 texts, 30 traditions, and 300 sources.

## Principles

- Public benefit: no paywall, advertising-led model, or conversion funnel.
- Non-sectarian presentation: distinguish traditional accounts, academic interpretations, and disputed claims.
- Source first: important historical claims should cite primary, archaeological, or scholarly sources.
- Uncertainty is data: support documented, probable, traditional account, disputed, and unknown states, including approximate dates.
- Human review: AI may extract, normalize, translate, summarize, or flag conflicts, but cannot invent citations or publish historical claims automatically.
- Open continuity: design for exportable data so the knowledge can survive the project.

## Planned stack

ASP.NET Core, PostgreSQL, React/Next.js, OpenStreetMap with MapLibre, custom React/D3 timeline, Cytoscape.js graph exploration, and PostgreSQL full-text search. Elasticsearch is deferred until real usage requires it.

## Status

The backend is implemented as a set of .NET class libraries delivered through the
OpenSpec workflow:

- A source-first domain model and EF Core persistence layer.
- Read paths for the timeline, time-filtered map, and multilingual entity search.
- A contribution-review workflow with immutable, field-level revisions.
- A versioned read-only public API (`/api/v1`) with bounded pagination, cache and
  rate-limit behavior, and reproducible bulk export (schema version + license).
- AI-assisted curation that produces immutable, provenanced drafts under a human
  publication gate (acceptance only promotes a draft into the contribution-review
  queue; duplicate and date-conflict suggestions never merge records).

All eight OpenSpec changes are archived. The interactive UI and runtime host
(React/Next.js, MapLibre, D3, Cytoscape.js) are not yet built. See
[ROADMAP.md](ROADMAP.md) for sequence and [HANDOFF.md](HANDOFF.md) for current
state.

## Scope boundary

The MVP does not include online temples, community features, live streaming, courses, donations, fortune telling, a Q&A bot, a full Buddhist canon reader, sectarian forums, temple reviews, or social features. Modern Buddhism (1800–present) and the 1000–1800 period follow after the MVP.

## Development

This repository uses OpenSpec for changes. Read [AGENTS.md](AGENTS.md) before working. The current validation command is:

```bash
openspec validate --all --strict --no-interactive
```
