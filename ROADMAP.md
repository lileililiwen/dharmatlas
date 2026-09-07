# Dharmatlas Roadmap

## Product direction

Build a source-first historical atlas, not a collection of unsourced articles. Each useful path should connect time, geography, entities, relationships, and sources.

## Phase 0 — Foundation

Target: repository and data contract ready for implementation.

- Establish project governance and OpenSpec workflow.
- Define the initial entity, name, relationship, event, source, claim, revision, and contributor boundaries.
- Establish date uncertainty and historical-certainty conventions.
- Add reproducible local validation.

OpenSpec change: `project-foundation`.

The next implementation queue is:

1. `historical-data-model`
2. `timeline-exploration`
3. `historical-map`
4. `entity-discovery-and-search`
5. `contribution-review-and-revisions`
6. `open-api-and-data-export`
7. `ai-assisted-curation`

## Phase 1 — MVP: 500 BCE–1000 CE

Target: first useful public atlas.

- Curated seed data for India, Central Asia, China, Sri Lanka, Tibet, Korea, and Japan.
- Timeline with zoom and region/category filters.
- Time-filtered historical map for places, institutions, routes, and archaeological sites.
- Person, place, and text pages.
- Search for canonical and alternative names across scripts and romanizations.
- Source records attached to historical claims.

The first four changes establish the MVP read path. Contribution review, open data, and AI assistance follow after the read path is useful; they remain separately scoped to avoid coupling publication governance and automation to the first UI release.

The MVP should prefer a small, beautiful, inspectable dataset over broad coverage. Institutions, traditions, contribution review, revision history, graph exploration, and public read-only API are sequenced behind the first usable atlas unless a change explicitly pulls one forward.

## Phase 2 — 1000–1800 CE

Expand coverage to Tibetan developments, mature East Asian traditions, Southeast Asian Theravada, Japanese Buddhism, and the decline of Buddhism in Central Asia.

## Phase 3 — 1800–present

Add Buddhist modernism, Western transmission, academic Buddhology, European and American Zen, Asian revivals, and international Buddhist organizations.

## Deferred scope

Defer full-text canonical databases, social/community features, online courses, streaming, donations, temple reviews, sectarian debate forums, automated publication, and Elasticsearch until a separately approved OpenSpec change justifies them.

## Quality gates

Every change must preserve source traceability, non-sectarian presentation, approximate-date support, reviewability, and exportability. Runtime claims require tests; OpenSpec changes require strict validation.
