# Design: Multilingual Search Fidelity

## Normalization

Pure `NameNormalizer`: Unicode NFKC, casefold, diacritic-strip layer for Latin/IAST, Pinyin tone-mark folding (`Xuánzàng` → `xuanzang`), Wylie tolerant match, CJK bigram tokenization for `tsvector`, Han variant folding where safe. Original form always preserved for display; normalized form indexed separately.

## Storage and ranking

New migration adds `normalized_value` + trigram/GIN indexes on entity names. Query pipeline: exact → normalized → transliteration-variant → trigram-fuzzy (threshold-gated). Score: primary boost, exact boost, type-ahead cap 100. Response adds `matchedName` + `matchKind` (additive fields).

## Benchmark fixture

`tests/Dharmatlas.Search.Tests/fixtures/multilingual.json`: ~120 cases across Sanskrit/Devanagari, Pali, Han, Tibetan/Wylie, Korean, Japanese, English + IAST/Pinyin/Wylie variants, each with expected canonical ID and minimum rank. Runner reports p50/p95 at limit=100 and prints plan-selectivity warnings.

## Verification

Unit: normalizer vectors per script/scheme. Integration: fixture pass rate 100%, p95 recorded, plan uses index or logs the exception. Privacy: no query logging with identity; benchmark uses synthetic terms.
