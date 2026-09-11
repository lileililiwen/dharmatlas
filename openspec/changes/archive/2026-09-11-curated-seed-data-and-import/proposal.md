# Proposal: Curated Seed Data and Import

## Why

The intended MVP has no committed corpus, so users cannot inspect the atlas and contributors cannot reproduce the same dataset.

## Scope

Define a versioned, source-linked import format and add a small representative seed corpus covering the MVP entity types, regions, uncertainty states, aliases, intervals, relationships, and sources. Imports must be deterministic and validate references before publication.

## What Changes

- Add the versioned seed manifest and representative source-linked corpus under `data/seed/v1/`.
- Add pure manifest parsing/validation, transactional database import, and a repeatable import CLI.
- Add tests and operations documentation for validation, idempotency, licensing, and safe reruns.

## Non-goals

- No attempt to complete all planned counts in one change.
- No unsourced historical facts.
- No web scraping or automatic AI publication.
- No replacement of application-owned data with external schemas.

## Dependencies

Depends on `runtime-host-and-deployment` for loading into a real database and on `entity-name-persistence-and-read-model` for name correctness. It may use existing domain/persistence contracts before those changes are complete, but final verification requires both.

## Success criteria

A clean environment can validate and load a pinned, reviewable seed snapshot; the same input produces the same IDs and export ordering; invalid source/entity references fail before any records are published.
