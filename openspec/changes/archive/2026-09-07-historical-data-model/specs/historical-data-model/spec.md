# Historical data model Specification

## ADDED Requirements

### Requirement: Core historical entities

The system MUST persist Person, Place, Institution, Text, Tradition, and Event records with stable identifiers and type-specific fields.

#### Scenario: Entity has stable identity

- **WHEN** an entity is renamed or gains an alias
- **THEN** its stable identifier remains unchanged and the names are retained as separate records

### Requirement: Source-linked claims

The system MUST represent historical assertions independently from entities and link each publishable assertion to one or more sources and one certainty value.

#### Scenario: Unsupported claim

- **WHEN** a claim has no source and is not an unpublished draft
- **THEN** publication is rejected

### Requirement: Approximate dates

The system MUST preserve human-readable date expressions and optional normalized bounds for exact, approximate, century, interval, and traditional dates.

#### Scenario: Interval filtering

- **WHEN** an event is recorded as between 150 and 250 CE
- **THEN** timeline queries can filter it by overlapping normalized bounds without replacing the displayed interval
