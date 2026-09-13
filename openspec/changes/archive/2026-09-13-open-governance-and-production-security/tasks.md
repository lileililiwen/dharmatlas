# Tasks

- [x] Add `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md` and link from README.
- [x] Implement OIDC reference handler with JWKS rotation and `sub`→contributor mapping; keep stub clearly dev-only.
- [x] Add CSP/HSTS/CORS middleware, non-root image, env-only secrets with fail-closed startup.
- [x] Add distributed rate-limit option and burst/deny tests incl. self-approval denial.
- [x] Enable Dependabot, audit gates, and secret-scan in CI.
- [x] Run strict OpenSpec validation and `git diff --check`.

## Delivery workflow

1. Select this change with `openspec list`.
2. Implement only this change and its tests.
3. Update this `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.
