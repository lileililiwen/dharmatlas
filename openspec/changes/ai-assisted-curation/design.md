# Design: AI-assisted curation

AI jobs operate only on supplied source material or existing records and produce typed suggestions. Each suggestion is immutable, linked to its input source/record, and labeled `AI Draft` until a human reviewer accepts, edits, or rejects it. The pipeline must preserve the original text, model/version, execution time, and reviewer decision. Conflicting dates and possible duplicate entities are queue items, not automatic merges. Published claims still pass the source and contribution review gates.
