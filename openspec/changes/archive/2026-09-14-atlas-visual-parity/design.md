# Design: Atlas Visual Parity

## Map pane

MapLibre GL with configurable `VITE_TILE_URL` + attribution. Query path reuses `GET /api/v1/map?year&bbox&includeUnknownActivity`; caps at 500 features; client clusters beyond threshold. Unknown-activity features render only when opted in, with hatched style + label. Tile failure degrades to list + notice, never blank.

## Timeline pane

D3 band chart: x = year (-500..1500 default), rows = region/category lanes. Interval dates are bands, approximate dates are dashed bands with `ca.` label, traditional dates carry a distinct glyph + tooltip. Zoom is wheel/buttons/keyboard; filters are region + category; selection navigates to `detailRoute`.

## Graph pane

Cytoscape.js neighborhood: center entity + 1-hop edges from relationships API, depth control capped at 2, edge labels show type + certainty. Click navigates; disputed edges show parallel edges, not merged. Large neighborhoods paginate with explicit "show more".

## Fallbacks and motion

Every pane ships a semantic `<ul>` fallback with identical data (title, date/activity expression, certainty). `prefers-reduced-motion` disables transitions; all controls are focus-visible with ARIA roles; color is never the sole certainty signal.

## Verification

Component tests for band rendering and fallback parity; contract tests for cap/clustering behavior; Playwright journeys (year drag, filter, graph hop) + axe; tile-outage injection proves list continuity.
