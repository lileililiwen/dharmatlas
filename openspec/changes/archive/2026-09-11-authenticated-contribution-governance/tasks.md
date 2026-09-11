# Tasks

- [x] Define provider-neutral identity-to-contributor and role contracts.
- [x] Add host authentication and contributor/reviewer authorization policies.
- [x] Add typed contribution and review request models with server-owned actor and timestamp fields.
- [x] Validate source existence, target type/existence, payload shape, and ownership before queue insertion.
- [x] Add concurrency token and transactional approve/revision/publication behavior.
- [x] Add moderation audit fields and safe contributor/reviewer views.
- [x] Add authorization, validation, self-approval, concurrency, rollback, and audit tests.
- [x] Verify with PostgreSQL integration tests, host authorization tests, strict OpenSpec validation, and `git diff --check`.
