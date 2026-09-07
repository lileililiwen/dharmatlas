# entity-discovery-and-search Specification

## Purpose
TBD - created by archiving change entity-discovery-and-search. Update Purpose after archive.
## Requirements
### Requirement: Multilingual entity search

Search MUST match canonical names, alternate scripts, transliterations, and romanizations while returning one stable entity identity.

#### Scenario: Romanized alias

- **WHEN** a user searches for `Hsuan-tsang`
- **THEN** the Xuanzang entity is returned with the matching alias identified

### Requirement: Inspectable entity pages

Entity pages MUST show relevant dates, relationships, uncertainty, and sources without presenting missing information as fact.

#### Scenario: Person detail

- **WHEN** a user opens a person page
- **THEN** names, active period, relationships, related texts/places, and source records are available when recorded

### Requirement: Search result provenance

Search results MUST identify entity type and matched name form.

#### Scenario: Ambiguous name

- **WHEN** multiple entities share a name
- **THEN** results distinguish them by type, period, region, or other recorded context

