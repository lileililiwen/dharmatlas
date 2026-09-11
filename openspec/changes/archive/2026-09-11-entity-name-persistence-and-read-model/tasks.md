# Tasks

- [x] Decide and document the authoritative `EntityName` write/persistence boundary.
- [x] Add required field length and normalized uniqueness validation.
- [x] Add a database uniqueness constraint for one primary name per entity/language.
- [x] Update import, contribution, and entity creation paths to persist names explicitly.
- [x] Centralize canonical-name, alias ordering, and missing-name projection.
- [x] Add PostgreSQL round-trip verification proving aliases survive save/reload.
- [x] Add tests for duplicate primary names, multilingual aliases, and consistent API/search/export output.
- [x] Verify with migration tests, focused tests, strict OpenSpec validation, and `git diff --check`.
