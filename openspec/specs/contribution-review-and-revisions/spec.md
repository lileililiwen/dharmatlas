# contribution-review-and-revisions Specification

## Purpose
Defines the human-reviewed contribution workflow: submissions remain pending
until review, approved changes publish transactionally, and immutable revisions
preserve reviewer, source, reason, and changed-field provenance.
## Requirements
### Requirement: Reviewed contribution workflow

The system MUST route contributor submissions through review before changing published historical data.

#### Scenario: Approved correction

- **WHEN** a reviewer approves a sourced date correction
- **THEN** the published record changes and the revision records the contributor, reviewer, source, reason, and changed fields

### Requirement: No direct publication

Contributors MUST NOT directly publish edits to formal historical records.

#### Scenario: Contributor submits an event

- **WHEN** a contributor submits a new event
- **THEN** it remains a draft or pending submission until a reviewer approves it

### Requirement: Complete revision history

The system MUST retain inspectable, ordered revisions for every published change.

#### Scenario: Historical revision inspection

- **WHEN** a reader opens an entity history
- **THEN** prior values, changed fields, actor, timestamp, reason, and sources can be inspected
