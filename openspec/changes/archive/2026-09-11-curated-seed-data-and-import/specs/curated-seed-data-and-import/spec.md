# Curated Seed Data and Import

## ADDED Requirements

### Requirement: Versioned reviewable corpus
The repository MUST contain a versioned seed manifest and representative source-linked MVP data using stable IDs.

#### Scenario: Reviewer inspects a seed record
- **WHEN** a reviewer opens a seed record
- **THEN** its entity type, names, date expression, certainty, sources, and licensing context are directly inspectable

### Requirement: Import validation
The importer MUST reject malformed records, duplicate IDs, invalid references, unsupported certainty values, inverted dates, and invalid coordinates before writing published data.

#### Scenario: Relationship references an unknown source
- **WHEN** validation encounters an unknown source ID
- **THEN** import fails with the file and record identifier and no partial publication occurs

### Requirement: Deterministic loading
Importing the same manifest twice MUST produce the same stable records and an equivalent export snapshot.

#### Scenario: Repeatable import
- **WHEN** the identical pinned manifest is imported into two empty databases
- **THEN** their published exports are equivalent except for explicitly generated timestamps

### Requirement: Uncertainty preservation
The corpus MUST preserve approximate, interval, traditional, disputed, and unknown states without converting them into false precision.

#### Scenario: Traditional date is imported
- **WHEN** a traditional date has an optional normalized estimate
- **THEN** the original expression and certainty remain visible alongside, not replaced by, the estimate
