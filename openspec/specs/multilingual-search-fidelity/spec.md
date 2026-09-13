# multilingual-search-fidelity Specification

## Purpose
TBD - created by archiving change multilingual-search-fidelity. Update Purpose after archive.
## Requirements
### Requirement: Script-aware normalization
Search MUST normalize case, diacritics, Pinyin tones, Wylie variants, and CJK tokenization while preserving the authored display form.

#### Scenario: Cross-script alias resolves
- **WHEN** a user searches `那爛陀`, `Nālandā`, or `Nalanda`
- **THEN** the same canonical institution is returned with the matched form shown

#### Scenario: Romanization variant resolves
- **WHEN** a user searches `Hsüan-tsang` or `玄奘`
- **THEN** the Xuanzang pilgrim record is returned with `matchKind` indicating transliteration match

### Requirement: Ranked bounded results
Results MUST rank primary over alias and exact over transliteration over fuzzy, capped at 100 items with stable ordering.

#### Scenario: Ambiguous term does not merge
- **WHEN** a normalized term matches multiple entities
- **THEN** each candidate appears as a separate hit with its matched and canonical names visible

### Requirement: Benchmarked adoption gate
A checked-in fixture MUST measure multilingual pass rate and p95 latency at maximum page size to justify future engine adoption objectively.

#### Scenario: Benchmark reports gate
- **WHEN** the benchmark runs against PostgreSQL
- **THEN** it reports pass rate plus p50/p95 and states whether the sustained 250ms adoption threshold is met

