# Dharmatlas Domain Contract

This document is the canonical, source-first domain contract for Dharmatlas. It
defines the initial vocabulary, date model, certainty model, and source-linking
rules that every later change (data model, timeline, map, entities, search,
review, export, curation) must obey. It is the human-readable companion to the
machine-checked `openspec/changes/project-foundation/specs/project-foundation/spec.md`.

The contract exists to prevent later data and UI work from encoding false date
precision, unsourced claims, or sectarian assumptions. Nothing here touches a
runtime backend or frontend; it is the agreed shape of the historical record.

## 1. Entity vocabulary

`Entity` is the common identity for the following concrete types:

- **Person** — an individual human actor (founder, translator, ruler, monk).
- **Place** — a geographic location (city, monastery, region, site, route).
- **Institution** — an organized body (monastery, order, university, council).
- **Text** — a written work (sutra, commentary, chronicle, inscription).
- **Tradition** — a lineage or school (Theravāda, Madhyamaka, Chan, etc.).
- **Event** — a happening situated in time and optionally place (council,
  translation project, migration).

Every `Entity` carries a stable canonical identifier plus one or more
`EntityName` records.

### EntityName

An `EntityName` stores the forms by which an entity is known:

- `language` — ISO 639 code (e.g., `san`, `chn`, `eng`).
- `script` — writing system (e.g., `devanagari`, `han`, `tibt`, `latin`).
- `romanization` — transliteration scheme applied (e.g., IAST, Pinyin, Wylie).
- `value` — the actual string in that script/romanization.
- `is_primary` — at most one primary name per language.

Alternate names let equivalent forms (a Sanskrit root, a Chinese rendering, a
Wylie romanization, an English gloss) resolve to the same canonical entity so
multilingual search returns one result.

## 2. Relationship vocabulary

A `Relationship` is a typed, **sourceable** edge connecting two entities. It
MUST carry at least one source reference and may carry a certainty value.

Initial relationship types (extensible, never closed):

- `born-at` (Person → Place)
- `died-at` (Person → Place)
- `authored` (Person → Text)
- `translated` (Person/Text → Text)
- `located-in` (Place/Institution → Place)
- `founded` (Person/Institution → Institution/Place)
- `pupil-of` / `teacher-of` (Person ↔ Person)
- `influenced` / `influenced-by` (Entity ↔ Entity)
- `part-of` (Entity → Tradition/Institution)
- `succeeded` (Entity → Entity, temporal ordering)
- `held-at` (Event → Place)

Relationships are never asserted without a source. A disputed relationship is
recorded as multiple conflicting sourceable edges, not a single merged claim.

## 3. Source

`Source` is a first-class bibliographic record, not an inline footnote. It
stores enough metadata to locate and verify the reference (title, author,
date, publisher/collection, identifier such as DOI or inscription number). A
`Claim` or `Relationship` links to sources by reference, never by embedding
free-text citations that cannot be inspected.

## 4. Claim

A `Claim` expresses a single historical assertion and links to one or more
`Source` records with a certainty value (see §6). Claims are separately
reviewable objects so that disagreement is visible rather than hidden.

## 5. Date representation (uncertainty-aware)

Dates MUST support all of the following without forcing false precision:

| Kind            | Example display   | Storage                                   |
|-----------------|-------------------|-------------------------------------------|
| Exact date      | `AD 402-05-20`    | ISO date + normalized bound               |
| Year            | `327 BCE`         | normalized year (`-327`)                  |
| Century         | `4th c. BCE`      | normalized century range                  |
| Open interval   | `after 1191 CE`   | one open bound                            |
| Approximate     | `ca. 150 CE`      | display flag + approximate normalized range |
| Traditional      | `Year 3 of X's reign` | traditional expression + best normalized estimate when available |

Rules:

- Store the **display expression** exactly as authored so historical nuance is
  preserved (e.g., a traditional regnal year is not silently converted to a
  single Gregorian year).
- Store **normalized numeric bounds** when derivable (BCE as negative years),
  used only for filtering and range queries — never shown to readers as fact.
- An interval stores **both bounds** and is displayed as an interval; the
  system MUST NOT select a single year from it.
- Approximate values are displayed with an explicit marker (`ca.`).
- Traditional dates keep their original notation; a normalized estimate is
  optional and clearly labelled as such.

## 6. Certainty values

The controlled certainty vocabulary is fixed for the foundation:

- **Documented** — supported by primary, archaeological, or strong scholarly evidence.
- **Probable** — best supported inference where direct evidence is partial.
- **Traditional Account** — preserved in a tradition's own narrative; not independently verified.
- **Disputed** — sources disagree; multiple interpretations remain inspectable.
- **Unknown** — no determinable status.

Rules:

- A claim MUST NOT collapse a `Disputed` or `Traditional Account` value into an
  unqualified fact in any presentation layer.
- A claim MAY link multiple sources with differing interpretations; the
  differing certainty values are retained, not averaged away.
- An assertion with no source and not explicitly marked as an unverified draft
  is **not eligible for publication** as a historical fact.

## 7. Validation and one-change workflow

The local validation baseline is:

```bash
openspec validate --all --strict --no-interactive
```

Repository hygiene also checks for stray whitespace and uncommitted scope:

```bash
git diff --check
git status --short
```

Implementation follows the one-change workflow defined in `AGENTS.md`:

1. Select exactly one active change (`openspec list`).
2. Implement only that change and its focused tests.
3. Update the change's `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1 (implementation, tests, archive, generated specs).
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2 (only the `HANDOFF.md` update).
9. Stop; do not start another change or push.

This contract is the acceptance baseline for `project-foundation`. Later
changes extend it; they do not relax the source-first, uncertainty-aware, or
non-sectarian guarantees stated here.
