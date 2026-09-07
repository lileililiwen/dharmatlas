# historical-map Specification

## Purpose
Defines the read-only historical map surface for Dharmatlas. Features (places, institutions, routes, archaeological sites) are time-filtered by a selected year against recorded activity intervals, with an explicit unknown-activity result state. Each feature exposes its type, activity expression, certainty, and source links for provenance; uncertain activity is shown with an explicit style and never presented as exact continuity. The surface returns a bounded viewport payload, supports clustering, feature selection, and entity navigation, and degrades to a readable list on tile/rendering failure.
## Requirements
### Requirement: Time-filtered historical features

The map MUST show only features whose recorded activity interval overlaps the selected historical period, while allowing an explicit unknown-date result state.

#### Scenario: Historical year selection

- **WHEN** a user selects 650 CE
- **THEN** active Buddhist places, institutions, routes, and sites for that period are shown

### Requirement: Feature provenance

Each historical map feature MUST expose its type, activity expression, certainty, and source links.

#### Scenario: Site inspection

- **WHEN** a user selects an archaeological site
- **THEN** the feature details include its historical status and inspectable sources

### Requirement: Map failure fallback

The system MUST provide a readable list or retry state when map tiles or rendering are unavailable.

#### Scenario: Tile service unavailable

- **WHEN** map tiles fail to load
- **THEN** the user can still inspect the filtered feature list and retry the map

