# Proposal: Public Atlas Web Experience

## Why

The repository has no interactive UI, so the timeline, map, search, entity, source, and uncertainty contracts cannot help the public.

## What Changes

- Add a React/Vite public client with search, timeline, map/list exploration, entity detail, source evidence, and uncertainty-aware presentation.
- Serve the built client from the ASP.NET host and include it in the production container build.
- Add responsive, keyboard-visible interaction states and frontend formatter tests.

## Scope

Build a responsive React/Next.js public read application with timeline, historical map, search/discovery, entity pages, source/evidence inspection, uncertainty-aware labels, loading/empty/error states, keyboard navigation, and map/list fallback.

## Non-goals

- No social, devotional, commercial, donation, course, or live features.
- No client-side publication or AI approval.
- No full canon reader.
- No visual precision that contradicts uncertain dates or geography.

## Dependencies

Depends on `runtime-host-and-deployment`, `public-read-api-completion`, `provenance-claims-and-evidence`, and representative data import.

## Success criteria

A first-time visitor can search an entity, understand its time/place/source context, compare uncertainty, and navigate without requiring specialist knowledge or a mouse.
