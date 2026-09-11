# Design: Curated Seed Data and Import

## Format

Store human-reviewable JSON or JSONL files under `data/seed/v1/`, with stable explicit IDs, entities, names, sources, claims, relationships, and import metadata. Every published historical assertion carries source IDs and certainty. Dates retain display expressions and normalized bounds.

## Import boundary

Implement a pure parser/validator followed by a transactional importer. Validation checks schema, enum values, duplicate IDs, primary-name uniqueness, required source references, entity endpoints, date bounds, coordinates, and allowed region/type vocabulary. The importer upserts only the pinned snapshot and never silently merges conflicting records.

## Provenance

Each record includes source locator metadata and a short editorial note where uncertainty or traditional attribution needs explanation. The corpus license and source licensing constraints are recorded in the snapshot manifest.

## Verification

Use fixture tests for validation failures, deterministic ID/export checks, transaction rollback, and representative end-to-end loading into PostgreSQL.
