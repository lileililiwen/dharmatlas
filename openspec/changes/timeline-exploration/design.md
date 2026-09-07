# Design: Timeline exploration

The timeline service queries normalized event bounds and returns stable event summaries with display dates, certainty, category, regions, and linked entities. Overlapping ranges must be included; unknown or wholly unnormalized dates remain discoverable through an explicit unknown-date filter. The UI provides continuous zoom plus practical presets (1000 years, 500 years, 100 years, 20 years, 1 year), category and region filters, keyboard-accessible event focus, and a detail route.

The timeline is read-only in this change. It consumes the historical data model and does not duplicate source or date semantics.
