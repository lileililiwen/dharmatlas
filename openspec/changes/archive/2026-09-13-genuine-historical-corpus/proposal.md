# Proposal: Genuine Historical Corpus

## What Changes

Replace the synthetic seed (`Seed Teacher`, `Seed Monastic University`) with a 50–100 record curated corpus of real MVP history (500 BCE–1000 CE): Buddha/Shakyamuni tradition-complex, Ashokan edicts, early councils (traditional-labeled), Faxian/Xuanzang itineraries, Nalanda, Dunhuang/Mogao, Longmen/Yungang, Anuradhapura/Mahavihara. Add an editorial handbook, source-quality tiers, dataset versioning with changelog and citable snapshot, and AI-citation verification tooling.

## Why

The current seed (14 entities, 3 sources, 3 claims) proves plumbing but not credibility. A public atlas with placeholder persons cannot be usable or genuine. Reviewers, scholars, and users need inspectable real records with competing interpretations kept separate.

## Scope (Boundary)

In scope: `data/seed/v2` manifest + importer extension, `docs/editorial-handbook.md`, source-tier metadata, dataset `version/changelog/DOI-stub`, claim-coverage gate (every published entity has >=1 resolvable source), AI draft citation-check report.
Out of scope: Phase 2/3 periods (1000 CE+), full canon text ingestion, automated publication, Elasticsearch, UI map/timeline rendering (see atlas-visual-parity).

## Use cases

- UC1: A student opens Ashoka and sees edict locations, date interval, certainty, and competing chronologies as separate claims.
- UC2: An editor submits a Xuanzang route correction with primary + scholarly sources; reviewer sees tier labels and accepts through the existing human gate.
- UC3: A researcher cites dataset `seed-2026-Q4` checksum in a paper and reproduces the export.

## Non-goals / Exceptions

- No hagiography presented as documented fact; devotional claims stay `TraditionalAccount` with explicit labels.
- No unsourced entity is publishable, even if "well known"; such rows fail import with a named rejection.
- No sectarian privileging: Theravada/Mahayana/Vajrayana origins are parallel claims, never merged.
- No scraping copyrighted translations; bibliographic metadata only unless public-domain/CC-licensed.

## Dependencies

Extends `curated-seed-data-and-import`, `provenance-claims-and-evidence`, `ai-assisted-curation`. No host/API contract break; additive seed schema only.

## Success criteria

Import is idempotent, coverage spans 7 MVP regions × 6 entity types with real records, every published claim resolves to >=1 inspectable source, strict validation passes, and a scholar-readable changelog exists.
