# Search fidelity

Multilingual retrieval is script-aware and transliteration-tolerant. The
authored name form is always preserved for display; matching works on derived
forms only.

## Normalization (`Dharmatlas.Domain.NameNormalizer`)

Pure and dependency-free so persistence, search, and tests share one
definition:

- Base: Unicode NFKC, lower-invariant casefold, per-character diacritic strip,
  compatibility folds (`ß`→`ss`, `æ`→`ae`, `œ`→`oe`, `ł`→`l`, `đ`→`d`,
  `ð`→`d`, `þ`→`th`, `ŋ`→`n`). Pinyin tone marks (`Xuánzàng`) and IAST
  macrons (`Nālandā`) fold to plain ASCII; fullwidth forms fold via NFKC.
- Hiragana/Katakana are never decomposed: voicing marks are contrastive
  (`が` ≠ `か`). Devanagari spacing vowel signs survive; the anusvara folds
  identically on both the query and the stored side.
- Transliteration key: separator removal plus documented Wade-Giles/Pinyin
  equivalences (`hs`→`x`, `tz`/`ts`→`z`, `-ien`→`-ian`), so `Hsüan-tsang`,
  `hsuantsang`, and `Xuanzang` share a key without ever becoming "exact".
- CJK bigrams over Han/Hangul/Kana/Tibetan runs for full-text indexing. Han
  variant (traditional/simplified) folding is limited to NFKC compatibility;
  `鳩摩羅什` and `鸠摩罗什` are stored as separate aliases, not merged.

## Ranking

Per name, best layer wins; per entity, best name wins (one stable hit per
identity). Primary names outrank aliases at every layer:

| Layer | Primary | Alias | `matchKind` |
|---|---|---|---|
| Exact normalized match | 100 | 80 | `Exact` |
| Transliteration-key match | 70 | 60 | `Transliteration` |
| Substring (either direction) | 40 | 30 | `Substring` |
| Edit distance ≤ 2, both sides ≥ 3 chars, length gap ≤ 2 | 20 | 15 | `Fuzzy` |

Ties break by entity type, region, then canonical name. Results are hard-capped
at 100 hits; the candidate prefilter caps at 1000. Ambiguous terms return every
candidate as a separate hit with `matchedName`, `matchedForm`, and `matchKind`
visible — identities are never merged.

## Storage

`entity_names.normalized_value` (populated by `DharmatlasDbContext` on every
insert/update) backs diacritic-insensitive candidate selection alongside the
raw `value` and the transliteration key. PostgreSQL adds `pg_trgm` GIN indexes
on both `normalized_value` and `value` (migration
`MultilingualSearchFidelity`); the btree index on `normalized_value` covers
providers without `pg_trgm`. Small corpora may still sequence-scan — expected,
not a regression (see benchmark below).

## Benchmark

`tests/Dharmatlas.Domain.Tests/fixtures/multilingual.json`: 11 entities,
53 cases across 6 scripts (latin, devanagari, hani traditional + simplified,
hangul, hiragana, tibetan) and 4 romanizations (IAST, Pinyin with tones,
Wylie, Wade-Giles; Hepburn and McCune-Reischauer probes included), each with
an expected canonical id and minimum rank, plus an ambiguous multi-hit case.
`MultilingualSearchBenchmarkTests` pads 500 distractors, runs every case at
`limit=100`, and reports pass rate with p50/p95.

Latest run: **53/53 pass, p50 0.92 ms, p95 2.46 ms** (in-memory engine).

## Adoption gate

A dedicated search engine (Elasticsearch/OpenSearch) is justified only when
the PostgreSQL-backed path shows **sustained p95 above 250 ms at the maximum
page size (limit=100)**, or query plans show the name indexes are no longer
selective for the published corpus. Until then PostgreSQL remains sufficient.
Benchmark queries are synthetic and checked in; no query text is logged with
identity.
