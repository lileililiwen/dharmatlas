# Public Atlas Web Experience

## ADDED Requirements

### Requirement: Usable public discovery
The web application MUST let a first-time visitor search multilingual names and navigate to a stable entity page.

#### Scenario: Visitor searches a romanized alias
- **WHEN** a visitor enters an alias
- **THEN** results show the matched form, entity type, certainty context, and a navigable detail link

### Requirement: Time and place exploration
The application MUST provide synchronized timeline and map exploration with filters and a non-map list fallback.

#### Scenario: Map tiles fail
- **WHEN** map tiles cannot load
- **THEN** the visitor can continue browsing the same bounded features in an accessible list

### Requirement: Honest uncertainty presentation
The UI MUST visibly preserve date intervals, approximation, traditional accounts, disputed interpretations, and unknown values.

#### Scenario: Event has an interval date
- **WHEN** an event spans a range
- **THEN** the UI displays the range and does not position it as a precise single-year fact

### Requirement: Accessible interaction
Core navigation and filtering MUST be usable by keyboard and assistive technology and MUST not depend on color alone.

#### Scenario: Keyboard-only exploration
- **WHEN** a visitor uses only keyboard input
- **THEN** they can search, filter, focus results, open details, and return without inaccessible traps
