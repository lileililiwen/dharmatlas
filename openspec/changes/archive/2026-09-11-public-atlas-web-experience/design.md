# Design: Public Atlas Web Experience

## Information architecture

Home explains the atlas and uncertainty vocabulary. Search is global and alias-aware. Entity pages prioritize identity, dates, geography, relationships, claims, and sources. Timeline and map share filters and deep links; map always has a synchronized list fallback.

## Visual and interaction rules

Use explicit labels for BCE/CE, approximate, traditional, disputed, and unknown. Never place uncertain points or intervals with misleading exactness. Preserve URL query state for shareable exploration. Provide loading, empty, error, no-geography, no-date, and tile-failure states.

## Accessibility

Use semantic headings/landmarks, keyboard-accessible filters and focus navigation, visible focus, text alternatives for map features, non-color certainty cues, responsive layout, and WCAG 2.2 AA automated plus manual checks.

## Verification

Use component tests, browser journeys, responsive snapshots, keyboard checks, axe/contrast checks, and API contract fixtures.
