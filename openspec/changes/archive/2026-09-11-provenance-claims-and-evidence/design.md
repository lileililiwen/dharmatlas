# Design: Provenance, Claims, and Evidence

## Model

Claims link a statement to a subject entity, certainty, status, source IDs, and optional source-specific note/locator. Entity and relationship projections expose claims or claim references without duplicating text inconsistently.

## Publication boundary

Only published claims appear publicly. Draft, rejected, and private moderation material remains internal. Publication validation requires existing sources and a certainty value; disputed interpretations are additive records rather than overwrites.

## Read experience

Entity pages and API responses provide an evidence section: statement, certainty badge, source title, locator, and an explicit distinction between traditional account and historical/academic interpretation. Missing evidence is represented as unknown, never silently omitted when the assertion is shown.

## Verification

Test source visibility, competing claims, publication filtering, source-reference integrity, export round trips, and API contract serialization.
