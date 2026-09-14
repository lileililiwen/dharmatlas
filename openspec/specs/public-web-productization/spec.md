# public-web-productization Specification

## Purpose
TBD - created by archiving change public-web-productization. Update Purpose after archive.
## Requirements
### Requirement: Deep-linkable routes
Every public entity MUST be reachable by a stable shareable route matching the API `detailRoute`, surviving reload and share.

#### Scenario: Shared link opens evidence
- **WHEN** a visitor opens `/places/{id}` from a share
- **THEN** the same entity, certainty labels, claims, and sources render as from in-app navigation

#### Scenario: Unknown ID is sourced
- **WHEN** an unknown entity ID is requested
- **THEN** a 404 with search guidance renders instead of a blank or draft leak

### Requirement: Indexable metadata
Entity pages MUST expose title, description, canonical URL, and OG tags plus sitemap entries for published records only.

#### Scenario: Crawler indexes published entity
- **WHEN** a crawler fetches a published entity route or sitemap
- **THEN** it receives indexable metadata with no draft or private content

### Requirement: I18n shell with safe fallback
UI chrome MUST support English plus additional locales with English fallback, without machine-translating historical claims.

#### Scenario: Missing translation falls back
- **WHEN** a locale lacks a chrome string
- **THEN** English renders instead of a blank, and claim text remains in its authored language

### Requirement: Offline revisit and private telemetry
Visited reads MUST be re-openable offline within a bounded TTL, and usage telemetry MUST contain no PII.

#### Scenario: Airplane-mode revisit
- **WHEN** a visitor reopens a previously viewed entity offline
- **THEN** cached claims and sources render with a stale indicator

#### Scenario: Telemetry rejects PII
- **WHEN** telemetry payloads are validated
- **THEN** any field carrying identity, query text with identity, or claim text fails the schema test

