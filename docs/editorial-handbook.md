# Editorial Handbook (Genuine Historical Corpus)

Version: seed-2026-Q4. Status: draft assistance only; every record requires
human review before publication. AI output must not invent citations.

## 1. Source tiers

Every source carries one tier. Claims and relationships inherit the tiers of
the sources they cite; reviewer surfaces show the tier next to each claim.

- `primary` — inscription, excavation report, or contemporaneous text
  (e.g. Ashokan edicts via Hultzsch, Epigraphia Indica).
- `scholarly` — peer-reviewed work or academic-press monograph
  (bibliographic metadata only for in-copyright works).
- `traditional` — chronicle or hagiography (Mahavamsa, pilgrim records where
  they transmit received tradition). Such claims are labeled
  `TraditionalAccount` and never render as `Documented`.
- `reference` — catalogue, gazetteer, or finding aid used for identifiers
  and coordinates.

Tier mismatch is a review flag, not an auto-fix: a `Documented` claim backed
only by `traditional` sources fails the citation check and stays pending.

## 2. Neutral tone

Summaries and statements use neutral, non-sectarian language.

- No superlatives or exclusivity: deny list includes `greatest`, `only true`,
  `supreme`, `perfect`, `highest`, `sole authentic`.
- No placeholder tokens: `Seed Teacher`, `Seed Monastic University`,
  `Seed Discourse`, `Seed Tradition` are rejected at import.
- Name the actors and the evidence; do not preach, rank schools, or present
  devotional claims as documented fact.

## 3. Competing interpretations

Disagreement is expressed as two separate claims, never one merged claim.

- Example: the introduction of Buddhism to Japan carries both a 538 CE claim
  and a 552 CE claim, each with its own sources and tiers.
- Example: the Buddha's dates carry a traditional account
  (c. 563–483 BCE) alongside a scholarly short-chronology interpretation
  (c. 480–400 BCE).
- Theravada, Mahayana, and Vajrayana origins are parallel claims, never merged
  and never ranked.

## 4. Date-expression style

- Use BCE/CE throughout; `ca.` marks approximation with normalized bounds.
- `Interval` requires both bounds; `Approximate` and `OpenInterval` require at
  least one bound; `Traditional` keeps the authored expression verbatim and
  carries the bounds scholars conventionally associate with it.
- Display expressions are preserved; normalized bounds are for filtering only.

## 5. License hygiene

- Bibliographic metadata (title, author, date, collection, identifier) may be
  recorded for any source.
- Full text only for public-domain or CC-licensed works (e.g. Legge 1886,
  Beal 1888, Geiger 1912, Hultzsch 1925). Otherwise cite; do not reproduce.
- Never scrape copyrighted translations.

## 6. Rejection catalog (importer error codes)

| Code | Meaning |
| ---- | ------- |
| `tier` | Unknown `sourceTier`; allowed: primary, scholarly, traditional, reference. |
| `resolvability` | Claim, relationship, or published entity with zero resolvable source IDs. |
| `tone` | Denied superlative or placeholder token in a summary or statement. |
| `coverage` | v2 corpus missing a region, type, or entity-source linkage (dry-run report). |
| `citation-check` | AI flag: missing source or tier mismatch; review-only, never publishes. |
