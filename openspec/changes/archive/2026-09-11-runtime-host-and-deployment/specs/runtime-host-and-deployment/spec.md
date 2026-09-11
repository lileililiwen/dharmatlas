# Runtime Host and Deployment Foundation

## ADDED Requirements

### Requirement: Executable composition root
The system MUST provide an executable ASP.NET Core host that registers the persistence context and existing public read services and maps the versioned public API.

#### Scenario: Fresh host starts with valid configuration
- **WHEN** the host starts with a valid PostgreSQL connection string
- **THEN** it starts without manual service registration and exposes `/api/v1/meta`

### Requirement: Explicit schema management
The system MUST use versioned EF migrations and MUST NOT rely on `EnsureCreated` for a deployed database.

#### Scenario: Migration command provisions an empty database
- **WHEN** an operator runs the documented migration command
- **THEN** all required tables and indexes are created and the migration history is recorded

### Requirement: Health separation
The system MUST expose liveness independent of the database and readiness that reports database connectivity.

#### Scenario: Database is unavailable
- **WHEN** the process is alive but PostgreSQL cannot be reached
- **THEN** liveness succeeds and readiness fails with an actionable status

### Requirement: Reproducible delivery
The repository MUST provide local startup instructions and CI gates for build, tests, OpenSpec validation, and host smoke behavior.

#### Scenario: Pull request validation
- **WHEN** CI runs on a change
- **THEN** it fails if build, focused tests, strict OpenSpec validation, or host smoke checks fail
