# atlas-visual-parity Specification

## Purpose
TBD - created by archiving change atlas-visual-parity. Update Purpose after archive.
## Requirements
### Requirement: Time-filtered map from live API
The map MUST render bounded `/api/v1/map` features on real tiles with clustering, caps, and an explicit unknown-activity opt-in.

#### Scenario: Year change updates features
- **WHEN** a visitor moves the reference year within -500..1500
- **THEN** visible features match the API response for that year and viewport, capped at 500 with a refinement prompt beyond the cap

#### Scenario: Tiles unavailable
- **WHEN** the tile service fails
- **THEN** the accessible feature list remains fully usable with a non-blocking notice

### Requirement: Uncertainty-honest timeline
The timeline MUST render interval and approximate dates as bands and traditional dates with a distinct marker, with zoom and region/category filters.

#### Scenario: Interval renders as band
- **WHEN** an event has lower and upper bounds
- **THEN** it displays as a band spanning the interval rather than a single point

### Requirement: Bounded relationship graph
The graph MUST show a 1-hop neighborhood (max 2-hop on request) from published relationships with typed, certainty-labeled edges.

#### Scenario: Disputed connection stays split
- **WHEN** two sources assert conflicting relations
- **THEN** the graph shows parallel edges with their own certainty rather than one merged edge

### Requirement: Accessible equivalence
Every visual pane MUST have a keyboard-navigable list equivalent with identical titles, dates, and certainty labels, honoring reduced-motion.

#### Scenario: Keyboard-only exploration
- **WHEN** a keyboard-only user explores map or timeline content
- **THEN** all information and navigation targets are reachable without pointer or motion-dependent cues

