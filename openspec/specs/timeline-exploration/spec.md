# timeline-exploration Specification

## Purpose
Defines the read-only timeline exploration surface for Dharmatlas. Events are queried by normalized date overlap, category, and region; approximate, interval, traditional, and unknown dates remain visible without inventing exact years. The surface supports keyboard focus, accessible labels, and non-visual equivalents for filter results, plus loading/empty/error states. It consumes the historical data model and does not duplicate source or date semantics.
## Requirements
### Requirement: Filterable historical timeline

The system MUST display events across the supported date range and filter them by date overlap, category, and region.

#### Scenario: Region and period filter

- **WHEN** a user selects 600–700 CE and China
- **THEN** the timeline shows matching Chinese events whose date ranges overlap that period

### Requirement: Uncertain dates remain visible

The system MUST display approximate, interval, traditional, and unknown dates without inventing exact years.

#### Scenario: Approximate event

- **WHEN** an event has an approximate date
- **THEN** its approximate expression and certainty state remain visible in the event summary

### Requirement: Accessible exploration

The timeline MUST support keyboard focus, readable event labels, and a non-visual equivalent for date and filter results.

#### Scenario: Keyboard event inspection

- **WHEN** a keyboard user focuses an event
- **THEN** the event summary, date, certainty, and detail navigation are available without pointer input

