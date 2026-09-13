# Open Governance and Production Security Specification

## Purpose
Enable lawful reuse and safe production operation through explicit licensing, contributor governance, verified identity, and hardened defaults.

## ADDED Requirements

### Requirement: Explicit open governance
The repository MUST include license, contribution, conduct, and security policies linked from the README.

#### Scenario: Reuser knows rights
- **WHEN** a third party reads the README and manifest metadata
- **THEN** code license, data license, and contribution terms are each stated without ambiguity

### Requirement: Verified production identity
Production authentication MUST map a stable OIDC subject to a contributor record and enforce role separation, failing closed when unconfigured.

#### Scenario: Anonymous write denied
- **WHEN** an unauthenticated request hits a contribution or review route
- **THEN** it is denied without creating state

#### Scenario: Self-approval denied
- **WHEN** a reviewer submits a decision on their own submission
- **THEN** the decision is rejected even if the reviewer holds the reviewer role

### Requirement: Hardened transport and secrets
The host MUST emit baseline security headers, run as non-root, and require secrets by environment with named fail-closed errors.

#### Scenario: Missing secret fails fast
- **WHEN** a required connection string or OIDC parameter is absent
- **THEN** startup fails with the missing key named and no default credential substituted

### Requirement: Bounded abuse surface
Write and heavy-read endpoints MUST enforce distributed rate limits and dependency scanning gates.

#### Scenario: Burst is throttled
- **WHEN** a client exceeds the search/export burst budget
- **THEN** further requests are throttled with a retry signal and no partial export is served as complete
