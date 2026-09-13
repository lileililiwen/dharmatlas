# Proposal: Multilingual Search Fidelity

## What Changes

Upgrade search from basic PostgreSQL matching to script-aware, transliteration-tolerant retrieval: CJK tokenization, IAST/Pinyin/Wylie normalization, alias-ranked results with matched-form + canonical display, plus a checked-in benchmark fixture and the decision gate for future search-engine adoption.

## Why

Buddhist names span Sanskrit/Devanagari, Pali, Chinese/Han, Tibetan, Korean, Japanese, and romanizations. Current search misses equivalent forms, so discovery fails for the exact multilingual promise in the domain contract.

## Scope (Boundary)

In scope: normalization library (`Dharmatlas.Search` extension), PostgreSQL trigram/full-text indexes where justified, ranking (primary > alias, exact > transliteration > fuzzy), `docs/search-fidelity.md` with supported scripts/schemes, benchmark fixture with p95 measurement at max page size.
Out of scope: Elasticsearch/OpenSearch deployment (only the objective adoption gate), machine translation, semantic/vector search, autocomplete analytics collection.

## Use cases

- UC1: Query `Nalanda`, `那爛陀`, or `Nālandā` resolves to one canonical institution.
- UC2: Query `Xuanzang` / `Hsüan-tsang` / `玄奘` returns the same pilgrim with matched-form shown.
- UC3: Maintainer runs the benchmark and gets a pass/fail against the 250ms p95 gate.

## Non-goals / Exceptions

- No silent transliteration merging: matched form is always shown; ambiguous merges surface as separate hits.
- No logging of query text with identity; benchmark queries are synthetic and checked in.
- No unbounded fuzzy expansion: edit-distance caps and result caps (<=100) hold under all inputs.
- No new public API version; ranking fields are additive to existing search DTOs.

## Dependencies

Extends `entity-discovery-and-search`, `entity-name-persistence-and-read-model`, `query-performance-and-export-delivery`. Independent of map/graph rendering.

## Success criteria

Fixture covers 6 scripts × 4 romanizations with expected canonical targets; all pass; benchmark reports p95 at max page size; query plans use selective indexes or record why not.
