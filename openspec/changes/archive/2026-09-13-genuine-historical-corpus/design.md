# Design: Genuine Historical Corpus

## Corpus shape

`data/seed/v2/manifest.json` keeps v1 schema (schemaVersion bump minor) with real records. Each entity: stable UUID, type, region, summary (neutral tone), names (primary + aliases), activity/date object preserving authored expression, certainty. Each claim: statement + certainty + interpretation + source_locator + >=1 sourceId. Each relationship: typed edge + certainty + >=1 sourceId. No free-text citations.

## Source tiers

`sourceTier`: `primary` (inscription, excavation report, contemporaneous text), `scholarly` (peer-reviewed / academic press), `traditional` (chronicle/hagiography labeled as such), `reference` (catalogue, gazetteer). Reviewer UI and API surface show tier; `traditional` never renders as `Documented`.

## Editorial handbook

`docs/editorial-handbook.md`: neutral-tone rules, no sectarian superlatives, competing-interpretation pattern (two claims, not one merged), date-expression style (BCE/CE, ca., interval), license hygiene (bibliographic-only for in-copyright works). Includes rejection catalog mapping to importer error codes.

## Importer changes

Extend `SeedManifestValidator` + `SeedImporter` with: tier enum check, coverage report (region × type matrix), source-resolvability check, neutral-tone lint (deny list: "greatest", "only true"), idempotent upsert by stable ID. Invalid rows abort the transaction with named errors; `--dry-run` prints coverage + rejections.

## Verification

Unit: tier validation, rejection paths, no-partial-write. Integration: double-import idempotence on Postgres, export checksum reproducibility, API spot-checks for Ashoka/Xuanzang/Nalanda evidence sections.
