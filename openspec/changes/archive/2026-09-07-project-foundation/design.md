# Design: Project foundation

## Domain boundary

Use a small shared vocabulary: `Entity` is the common identity for Person, Place, Institution, Text, Tradition, and Event. `EntityName` stores language, script, romanization, and alternate forms. `Relationship` connects two entities with a typed, sourceable edge. `Source` is a first-class bibliographic record. `Claim` expresses a historical assertion and links to one or more sources with a certainty label. `Revision` and `Contributor` support review history but do not imply direct publication rights.

## Temporal model

Dates must support exact dates, years, centuries, open intervals, approximate values, and traditional dates. Store the displayed expression as well as normalized bounds when possible. The normalized bounds support filtering; the expression preserves historical nuance.

## Certainty model

The initial controlled values are `Documented`, `Probable`, `Traditional Account`, `Disputed`, and `Unknown`. A claim may have multiple sources with differing interpretations. The UI must not collapse a disputed or traditional claim into an unqualified fact.

## Validation and delivery

The foundation change is documentation and contract work only. Its acceptance gate is strict OpenSpec validation plus repository hygiene checks. Runtime tests begin with the first implementation change and must remain focused on that change.
