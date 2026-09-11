# observability-and-release-quality Specification

## Purpose
Make failures detectable and recoverable through privacy-safe telemetry,
real-infrastructure release gates, and repeatable operational recovery
procedures.
## Requirements
### Requirement: Actionable telemetry
The host MUST emit privacy-safe structured logs and metrics for request failures, readiness, slow queries, imports, moderation, AI drafts, and exports with correlation identifiers.

#### Scenario: Public API request fails
- **WHEN** a request returns a server error
- **THEN** operators can correlate the response with a structured event without logging private source or contributor content

### Requirement: Real-infrastructure verification
Release gates MUST include applicable PostgreSQL, HTTP, browser, accessibility, migration, and export checks rather than relying only on in-memory unit tests.

#### Scenario: Migration breaks PostgreSQL startup
- **WHEN** CI applies migrations to a clean PostgreSQL database
- **THEN** the release gate fails before deployment and reports the migration error

### Requirement: Recovery documentation
The repository MUST document and periodically verify backup, restore, rollback, and failure-recovery procedures.

#### Scenario: Database restore is needed
- **WHEN** an operator follows the restore runbook in a non-production verification environment
- **THEN** the database and public read path return to a known-good snapshot
