# public-read-api-completion Specification

## Purpose
Defines the completed versioned public API routes for search, timeline, map,
institution, tradition, claim/evidence, entity detail, source navigation, and
bounded public reads over published data.
## Requirements
### Requirement: Complete discovery surface
The public API MUST expose documented search, timeline, map, institution, tradition, claim/evidence, entity detail, and source navigation routes.

#### Scenario: User searches for an alias
- **WHEN** a client requests a multilingual search term
- **THEN** the response identifies the stable entity and matched name form and provides a detail link

### Requirement: Bounded historical queries
Timeline and map endpoints MUST support bounded time, geography, category/type, certainty, and pagination/filter parameters appropriate to their surface.

#### Scenario: User explores a century and region
- **WHEN** a client requests an interval and region
- **THEN** only overlapping, eligible records are returned with their original date expressions and uncertainty labels

### Requirement: Stable versioned contracts
All public routes MUST carry versioned, documented response shapes and consistent problem responses for invalid input, missing records, and rate limits.

#### Scenario: Invalid map bounds
- **WHEN** a client submits invalid viewport or time bounds
- **THEN** the API returns a structured client error without querying unbounded data
