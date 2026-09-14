# Proposal: Atlas Visual Parity

## What Changes

Deliver real MapLibre time-filtered map, D3 zoomable timeline, and Cytoscape.js relationship graph on top of existing bounded read APIs, each paired with an accessible list fallback. Replaces CSS-dot `map-dots` and single-slider list with tiled geography, interval rendering, and entity-graph navigation.

## Why

The product promise is time + geography + connections. Current dots and lists do not let users answer "what was here then" or "how are these connected", so the atlas is not usable for its core job.

## Scope (Boundary)

In scope: `web` map pane (vector tiles, viewport/year query, clustering, unknown-activity opt-in), timeline pane (zoom, region/category filters, interval/approximate display), graph pane (1-hop neighborhood from relationships API, depth cap), list fallbacks, reduced-motion and keyboard paths.
Out of scope: offline tile packs, 3D terrain, full-text canon reader, social layers, new API version (reuses `/api/v1/map`, `/timeline`, relationships).

## Use cases

- UC1: A visitor drags the year to 650 CE over Central Asia and sees Xuanzang-route places appear/disappear with certainty badges.
- UC2: A keyboard-only user explores the same timeline content as a list without losing information.
- UC3: A reader on a tradition page opens its graph and jumps to a teacher, text, and place in one hop each.

## Non-goals / Exceptions

- No tile-vendor lock-in: tile URL is config, fallback list works with tiles unavailable.
- No false precision: approximate/interval dates render as bands, never points, unless explicitly exact.
- No >500 map features per response; clustering or explicit refinement is required beyond the cap.
- No motion-dependent meaning: reduced-motion disables animation without removing data.

## Dependencies

Builds on `historical-map`, `timeline-exploration`, `public-read-api-completion`, `public-atlas-web-experience`, `query-performance-and-export-delivery`. Needs genuine corpus data to be meaningful but is independently shippable.

## Success criteria

Map, timeline, and graph render from live API data against real tiles; empty/loading/error states work; keyboard + screen-reader + reduced-motion paths verified; production container includes tile-config without secrets.
