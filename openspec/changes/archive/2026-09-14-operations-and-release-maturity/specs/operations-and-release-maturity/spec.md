# Operations and Release Maturity Specification

## Purpose
Detect production failures quickly and recover repeatably through traced telemetry, SLO alerts, signed promotion, and drilled backup/restore, without expanding PII collection.

## ADDED Requirements

### Requirement: Traced privacy-safe telemetry
Requests, slow queries, imports, moderation, AI drafts, and exports MUST emit correlated traces and scrubbed errors linking to the existing correlation ID.

#### Scenario: Server error is traceable
- **WHEN** a public request returns 5xx
- **THEN** operators can join log, trace, and error report by correlation ID with no source text or identity present

#### Scenario: Scrub blocks PII
- **WHEN** an exception contains contributor email or claim text
- **THEN** the reporter redacts those fields before transmission and the scrub test passes

### Requirement: SLO alerts with runbooks
Release-critical signals MUST have SLOs, alert rules, and runbook links for readiness, error rate, latency, and export/restore health.

#### Scenario: Readiness failure pages correctly
- **WHEN** readiness checks fail beyond threshold
- **THEN** the alert fires with the rollback/restore runbook URL attached

### Requirement: Signed promotion without data auto-rollback
Releases MUST promote signed, SBOM-attested images through staging with migration pre-checks, retaining the prior image; production data rollback is restore-from-backup only.

#### Scenario: Bad migration stops promotion
- **WHEN** staging migration or journey checks fail
- **THEN** promotion halts and production remains on the previous signed image

### Requirement: Drilled recovery and journey gates
Backup/restore MUST be verified on schedule in a disposable database, and e2e/a11y/load gates MUST block regressing releases.

#### Scenario: Nightly restore proves recovery
- **WHEN** the scheduled restore drill runs
- **THEN** `/health/ready`, one published entity, and export checksum all verify against the restored snapshot
