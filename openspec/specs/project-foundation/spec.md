# project-foundation Specification

## Purpose
Defines the source-first domain contract for Dharmatlas: the entity and relationship vocabulary, uncertainty-aware date model, historical-certainty values, source-linking requirements, and the validation/one-change workflow. This is the acceptance baseline that every later change must extend without relaxing.
## Requirements
### Requirement: Source-first historical claims

The system design MUST represent a historical claim as a separately reviewable object that can link to one or more sources and carry a historical-certainty value.

#### Scenario: Claim has supporting sources

- **WHEN** a contributor records a historical assertion
- **THEN** the assertion can be stored with one or more source references and a certainty value

#### Scenario: Claim lacks evidence

- **WHEN** an assertion has no source and is not explicitly marked as an unverified draft
- **THEN** it MUST NOT be eligible for publication as a historical fact

### Requirement: Uncertainty-aware time

The system MUST represent exact, approximate, century-level, interval, and traditional dates without requiring false precision.

#### Scenario: Approximate date

- **WHEN** an event is known only as approximately 150 CE
- **THEN** the system preserves an approximate display value and supports a normalized searchable range when available

#### Scenario: Date interval

- **WHEN** an event is known to fall between two years
- **THEN** the system stores both bounds and displays the interval rather than selecting one exact year

### Requirement: Non-sectarian certainty presentation

The system MUST distinguish documented, probable, traditional account, disputed, and unknown historical status values.

#### Scenario: Disputed account

- **WHEN** sources disagree about a claim
- **THEN** the claim is displayed as disputed and the relevant sources remain inspectable

### Requirement: Multilingual entity identity

The system MUST support alternate entity names with language, script, and romanization metadata so equivalent forms can be searched together.

#### Scenario: Alternate name search

- **WHEN** a user searches for an entity using an alternate script or romanization
- **THEN** the matching canonical entity is returned

