# Design: Query Performance and Export Delivery

## Query path

Compose `IQueryable` filters before execution, use deterministic keyset pagination, select only required projection columns, and apply hard limits to map feature counts and timeline results. Search begins with PostgreSQL full-text/trigram capabilities only where benchmark evidence supports them.

## Export path

Create an export snapshot record with schema version, dataset revision, generated time, license, row counts, and SHA-256 checksum. A bounded background job streams ordered records to compressed storage and exposes status plus a download URL; failed jobs are retryable and never publish partial snapshots as complete.

## Verification

Use PostgreSQL `EXPLAIN` assertions or captured plans, large synthetic datasets, allocation/time budgets, pagination consistency tests, export checksum tests, and cancellation/retry tests.
