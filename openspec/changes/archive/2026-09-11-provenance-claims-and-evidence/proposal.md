# Proposal: Provenance, Claims, and Evidence

## Why

The project promises source-first history, but claims are not currently included in public detail responses or exports, and person/event/text source links are incomplete.

## Scope

Make claims first-class across persistence, publication, entity details, API, export, and presentation contracts. Support multiple source-backed interpretations, explicit certainty, traditional-account labeling, source locators, and evidence inspection.

## What Changes

- Add claim interpretation and source-locator metadata with publication-safe filtering.
- Expose published evidence on entity detail/API responses and bulk exports.
- Preserve conflicting and traditional claims as separately labeled records.

## Non-goals

- No automatic source quality scoring.
- No single authoritative ranking of sectarian interpretations.
- No full-text canonical reader.
- No AI-generated citation acceptance.

## Dependencies

Depends on `entity-name-persistence-and-read-model` and `public-read-api-completion`; seed fixtures from `curated-seed-data-and-import` should be used for acceptance tests.

## Success criteria

Every public material historical assertion can be traced to one or more inspectable sources, while conflicting and traditional accounts remain distinct.
