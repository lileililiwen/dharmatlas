# Query Performance and Export Delivery

## ADDED Requirements

### Requirement: Database-side bounded queries
Public list, search, timeline, and map queries MUST apply filters, ordering, projection, and limits before materializing results.

#### Scenario: Large entity table is searched
- **WHEN** a client requests one page of results
- **THEN** the database query returns only the bounded projection needed for that page

### Requirement: Stable pagination
Paged results MUST use deterministic ordering and a cursor that cannot duplicate or skip records under normal append-only publication.

#### Scenario: Client follows a cursor
- **WHEN** a client requests the next page using the returned cursor
- **THEN** records continue after the prior page in stable order

### Requirement: Reliable export snapshots
Large exports MUST be generated as identifiable, checksummed, compressed snapshots with observable status and retry-safe failure behavior.

#### Scenario: Export job fails midway
- **WHEN** storage or serialization fails
- **THEN** the job is marked failed, partial output is not advertised as complete, and retry can resume or restart safely
