# genuine-historical-corpus Specification

## Purpose
TBD - created by archiving change genuine-historical-corpus. Update Purpose after archive.
## Requirements
### Requirement: Real source-linked corpus coverage
The v2 seed MUST provide real MVP records spanning all seven regions and six entity types, with every published entity carrying at least one resolvable source reference.

#### Scenario: Placeholder-free import
- **WHEN** the v2 manifest is imported
- **THEN** no entity summary contains placeholder tokens such as "Seed Teacher" and the region-by-type coverage matrix is complete

#### Scenario: Unsourced record rejected
- **WHEN** a manifest row has zero resolvable source IDs
- **THEN** validation fails with a named rejection and the transaction writes nothing

### Requirement: Source tiers and non-sectarian labeling
Claims and relationships MUST carry a source tier, and traditional accounts MUST remain labeled separately from documented history.

#### Scenario: Traditional account stays labeled
- **WHEN** a council or hagiographic claim is published from a chronicle source
- **THEN** its certainty renders as a traditional account with tier `traditional` and never as documented fact

#### Scenario: Competing interpretations coexist
- **WHEN** sources disagree on dating or attribution
- **THEN** both claims persist as separate inspectable records with their own sources and tiers

### Requirement: Dataset citability
The dataset MUST expose a version, changelog, license note, and reproducible export checksum so external work can cite it.

#### Scenario: Citation reproduces export
- **WHEN** a cited dataset version is exported twice
- **THEN** both exports share the same checksum for the same code and manifest inputs

### Requirement: AI citation verification is review-only
Automated citation checks MUST flag missing or tier-mismatched sources without publishing or merging records.

#### Scenario: AI flag does not publish
- **WHEN** the checker flags a draft with an unverifiable citation
- **THEN** the draft stays pending and only a human review decision changes its state

