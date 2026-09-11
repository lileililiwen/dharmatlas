# entity-name-persistence-and-read-model Specification

## Purpose
TBD - created by archiving change entity-name-persistence-and-read-model. Update Purpose after archive.
## Requirements
### Requirement: Durable aliases
Every accepted entity name MUST be persisted as an `EntityName` record and MUST survive a database round trip.

#### Scenario: Alias is added and reloaded
- **WHEN** an editor adds a Sanskrit or Chinese alias and reloads the entity
- **THEN** the alias remains available to search, detail, and export projections

### Requirement: Primary-name uniqueness
The system MUST allow at most one primary name per entity and language at both validation and persistence boundaries.

#### Scenario: Two primary English names race
- **WHEN** two writes attempt to set different primary English names
- **THEN** one is rejected with an actionable conflict and the database remains invariant-safe

### Requirement: Consistent projection
Search, API, detail, and export MUST use the same deterministic canonical-name and alias ordering rules.

#### Scenario: Entity has aliases but no primary
- **WHEN** an entity is projected
- **THEN** the documented fallback alias is used consistently and is not replaced silently by the GUID
