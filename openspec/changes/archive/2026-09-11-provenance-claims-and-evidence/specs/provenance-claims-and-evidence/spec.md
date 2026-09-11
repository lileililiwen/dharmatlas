# Provenance, Claims, and Evidence

## ADDED Requirements

### Requirement: Source-traceable assertion
Every published material historical assertion MUST expose at least one inspectable source reference and one certainty value.

#### Scenario: Reader opens an entity claim
- **WHEN** a reader views a published claim
- **THEN** the statement, certainty, source record, and locator are available without consulting private moderation data

### Requirement: Competing interpretations
The system MUST preserve multiple source-backed interpretations as separate claims and MUST NOT choose one solely because it was entered later.

#### Scenario: Sources disagree about a date
- **WHEN** two published claims provide incompatible dates
- **THEN** both claims remain visible with their separate sources and certainty labels

### Requirement: Traditional account separation
Traditional accounts MUST be visibly labeled as traditional accounts and MUST NOT be rendered as documented historical fact.

#### Scenario: Traditional narrative is published
- **WHEN** a traditional-account claim is displayed
- **THEN** its label and source context distinguish it from documented or probable claims
### Requirement: Publication filtering
Draft, rejected, and private claims MUST NOT appear in public entity responses or exports.

#### Scenario: Rejected claim is queried
- **WHEN** a rejected claim is present in persistence
- **THEN** public detail and export responses exclude it
