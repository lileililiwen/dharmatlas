# Design: Open Governance and Production Security

## Governance files

`LICENSE` (code: MIT or Apache-2.0; data: CC-BY-4.0 noted in README + manifest), `CONTRIBUTING.md` (OpenSpec one-change workflow, DCO sign-off, seed licensing rules), `CODE_OF_CONDUCT.md` (report path, no sectarian harassment carve-outs), `SECURITY.md` (supported versions, 90-day disclosure, no private-data bounty scope).

## Auth reference

Replace stub with OIDC JWT-bearer reference: `Issuer/Audience` from env, JWKS with rotation, `sub` → `contributors.external_subject` mapping (existing), role claim mapping to contributor/reviewer. Anonymous writes denied; self-approval denied; decisions carry server correlation ID. Local dev keeps a documented loopback stub clearly marked non-production.

## Hardening

Middleware: CSP (no inline scripts beyond nonce/hashes), HSTS (prod only), CORS allowlist, forwarded-headers from trusted proxy only. `Dockerfile` gains `USER app`, read-only FS where possible. Rate limit: sliding-window backed by Postgres/Redis option with fail-closed behavior; per-key + per-IP tiers for search/map/export.

## Supply chain

Dependabot for NuGet + npm, `dotnet audit` / `npm audit` gates, secret-scan (gitleaks) in CI. No committed connection strings; startup validates required env and fails with named keys.

## Verification

Auth tests against stub JWKS (valid/expired/wrong-aud); header assertions; rate-limit burst test; image runs as non-root; audit/scan gates green on clean tree.
