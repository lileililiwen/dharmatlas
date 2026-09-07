# Design: Historical data model

Use a shared `Entity` identity with typed subtypes: Person, Place, Institution, Text, Tradition, and Event. Store `EntityName` records for language, script, romanization, and aliases. Store relationships as typed, directional, sourceable edges. Events use a display expression plus optional normalized lower and upper bounds, allowing `c. 150 CE`, centuries, intervals, and traditional dates. Claims are first-class records with certainty, status, and source links; revisions retain the prior value, contributor, reason, and timestamp.

The implementation should expose repository/service contracts independently of the future UI. Invalid references, inverted ranges, unsupported certainty values, and publication of unsupported claims must be rejected at the domain boundary.
