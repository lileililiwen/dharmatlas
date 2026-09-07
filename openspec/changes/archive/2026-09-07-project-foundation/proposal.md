# Change: Project foundation

## Why

Dharmatlas needs a stable, source-first domain contract and repository workflow before Timeline, Map, and entity features are implemented. Without this foundation, later data and UI work could encode false date precision, unsourced claims, or sectarian assumptions.

## What changes

- Establish the initial domain vocabulary for entities, names, relationships, events, sources, claims, revisions, and contributors.
- Define uncertainty-aware dates and historical certainty values.
- Establish the local validation baseline and project documentation contract.

## Non-goals

- No production backend, frontend, database migration, seed dataset, authentication, or public API.
- No automated AI publication workflow.
- No full Timeline or Map implementation.
