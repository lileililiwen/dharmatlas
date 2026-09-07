# Open API and data export Specification

## ADDED Requirements

### Requirement: Public read-only access

The system MUST expose versioned read-only access to published people, events, places, texts, and relationships.

#### Scenario: Fetch event data

- **WHEN** a client requests a published event by ID
- **THEN** the response includes its date expression, normalized bounds when available, location, participants, certainty, and sources

### Requirement: Stable export

The system MUST provide bulk snapshots with stable identifiers, schema version, license metadata, and source references.

#### Scenario: Project outage

- **WHEN** the hosted application is unavailable
- **THEN** a previously downloaded snapshot remains sufficient to identify records, relationships, and their sources

### Requirement: Published-data boundary

The public API and export MUST exclude drafts, rejected contributions, and private contributor data.

#### Scenario: Pending submission

- **WHEN** a record is awaiting review
- **THEN** it does not appear in public API responses or bulk snapshots
