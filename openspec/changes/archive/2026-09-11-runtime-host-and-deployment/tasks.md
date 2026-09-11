# Tasks

- [x] Create the executable ASP.NET Core host and composition root.
- [x] Register PostgreSQL DbContext, read services, API endpoints, and health checks.
- [x] Add initial EF migration and an explicit migration command/mode.
- [x] Add local PostgreSQL/host container configuration without production secrets.
- [x] Add CI restore, build, unit test, strict OpenSpec validation, migration, and smoke gates.
- [x] Document configuration, startup, migration, readiness, backup, and rollback procedures.
- [x] Add host integration tests for `/health/live`, `/health/ready`, and `/api/v1/meta`.
- [x] Verify with focused tests, PostgreSQL integration tests, `openspec validate --all --strict --no-interactive`, and `git diff --check`.
