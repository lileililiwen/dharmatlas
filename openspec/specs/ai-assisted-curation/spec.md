# ai-assisted-curation Specification

## Purpose
Defines the auditable AI-assistant layer for curation: every suggestion is an immutable, provenanced draft (input, model/version, output, confidence, reviewer decision) that can never publish on its own. Human review gates all publication — acceptance only promotes a draft into the existing contribution-review queue, and duplicate/date-conflict suggestions are review-only and never merge records.
## Requirements
### Requirement: Provenanced AI drafts

The system MUST retain the source input, model/version, generated suggestion, timestamp, and confidence for every AI-assisted draft.

#### Scenario: Entity extraction

- **WHEN** AI extracts a person from a supplied academic source
- **THEN** the result is stored as a draft linked to that source and cannot directly become a published entity

### Requirement: Human publication gate

AI-generated historical content MUST require human review before publication.

#### Scenario: Citation cannot be verified

- **WHEN** a reviewer cannot verify a generated citation
- **THEN** the draft is rejected or returned for correction and no published claim is created

### Requirement: Conflict suggestions are non-destructive

AI date-conflict and duplicate suggestions MUST not silently alter or merge published records.

#### Scenario: Possible duplicate

- **WHEN** AI identifies two possibly identical people
- **THEN** it creates a review suggestion while preserving both records and their existing sources

