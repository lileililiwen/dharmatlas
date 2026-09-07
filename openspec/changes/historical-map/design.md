# Design: Historical map exploration

The map consumes georeferenced entities and activity intervals from the historical data model. A selected year or interval determines which nodes and routes are active; uncertain activity is shown with an explicit certainty style and never presented as exact continuity. MapLibre renders clustered points at low zoom and typed layers at high zoom. Selecting a feature opens its entity detail and source summary. The service must return a stable, bounded viewport payload and degrade to a list when map tiles or geolocation rendering fail.
