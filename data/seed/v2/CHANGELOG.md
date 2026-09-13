# Seed Dataset Changelog

## seed-2026-Q4 (schema 1.1.0)

- Replaces the v1 representative seed (14 entities, 3 sources) with a genuine
  historical corpus: **53 entities, 75 names, 13 sources, 44 claims,
  14 relationships** spanning 500 BCE–1000 CE.
- Coverage: all seven MVP regions (India, Central Asia, China, Sri Lanka,
  Tibet, Korea, Japan) and all six entity types (Person, Place, Institution,
  Text, Tradition, Event).
- Every source carries a tier (primary, scholarly, traditional, reference);
  every entity has a resolvable source linkage; competing interpretations
  (Buddha dates, Japan 538 vs 552, First Council memory vs reconstruction)
  persist as separate claims.
- Citable snapshot: dataset version `seed-2026-Q4`, DOI stub
  `doi:10.0000/dharmatlas.seed-2026-Q4` (reserved, not registered). Exports of
  this manifest reproduce the same checksum for identical code inputs.
- Supersedes `seed-2026-09-11` (v1 placeholders removed; no `Seed *` tokens).
- Known limits: coordinates are gazetteer-grade, not survey-grade; Tibetan and
  Korean records lean on few sources; post-1000 CE material is out of scope.
- Operator note: the importer upserts by stable ID and never deletes. A
  database holding v1 rows keeps both corpora (duplicate canonical names are
  possible). Production cutover to v2 should import into a fresh database
  after `dotnet ef database update` (migration `SourceTiers` adds the nullable
  `sources.tier` column).
