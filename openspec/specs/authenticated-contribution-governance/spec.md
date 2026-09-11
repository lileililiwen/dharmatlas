# authenticated-contribution-governance Specification

## Purpose
Defines provider-neutral authenticated contribution and review governance: map
verified subjects to persisted contributor roles, protect write routes, validate
references, prevent self-approval, and record concurrency-safe audit metadata.
## Requirements
### Requirement: Verified actor identity
Contribution and review operations MUST use an authenticated principal mapped server-side to a contributor and role; request bodies MUST NOT select the effective actor.

#### Scenario: Anonymous review attempt
- **WHEN** an unauthenticated client submits a review request
- **THEN** the request is denied and no submission or revision changes

### Requirement: Reference-safe submissions
Submissions MUST validate typed payloads, source existence, target compatibility, and contributor authorization before entering review.

#### Scenario: Unknown source reference
- **WHEN** a contribution cites a source ID absent from the database
- **THEN** it is rejected before queue insertion with a field-specific error

### Requirement: Atomic review publication
Approval, publication, decision recording, and revision creation MUST be one transactional operation protected against concurrent decisions.

#### Scenario: Two reviewers approve the same submission
- **WHEN** concurrent approvals target one pending submission
- **THEN** exactly one succeeds and the other receives a conflict without duplicate publication

### Requirement: Human governance
The system MUST retain reviewer identity, reason, timestamp, and status history, and MUST deny self-approval by default.

#### Scenario: Contributor reviews own submission
- **WHEN** a contributor without an override attempts to approve their submission
- **THEN** authorization denies the action and records no approval
