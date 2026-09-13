# Proposal: Open Governance and Production Security

## What Changes

Make the repo adoptable and deployable: add `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`; provide a reference OIDC integration replacing `UnconfiguredAuthenticationHandler`; add CSP/HSTS/CORS, secrets-by-environment, non-root container, distributed rate limiting, and Dependabot + secret-scan gates.

## Why

Missing license blocks all reuse. Stub auth, in-memory rate limits, absent security headers, and hardcoded local creds block any real deployment with contributors.

## Scope (Boundary)

In scope: license selection (e.g., MIT/Apache-2.0 code + CC-BY-4.0 data note), contributor workflow + DCO/CLA note, conduct + reporting path, security policy + disclosure window, OIDC reference handler with role mapping + key rotation docs, header middleware, env-only secrets validation, `USER app` image, Redis/Postgres-backed rate limit option, Dependabot + `npm/dotNet audit` gates.
Out of scope: operating a public IdP, SSO for all providers, WAF/DDoS vendor, pen-test certification, production cluster Terraform (see operations change).

## Use cases

- UC1: A university forks the repo knowing code vs data licensing and contribution rules.
- UC2: A deployment sets OIDC issuer + role claim mapping and gets contributor/reviewer enforcement without code edits.
- UC3: A scanner flags a vulnerable dependency and CI fails before merge.

## Non-goals / Exceptions

- No trust of `X-User-Id` / role headers from clients; server maps stable OIDC `sub` only.
- No self-approval even for admins; dual-control holds as an exception-free rule.
- No secrets in git, logs, or exports; startup fails closed with a named missing-config error.
- No weakening of source/review gates for contributor convenience.

## Dependencies

Extends `authenticated-contribution-governance`, `runtime-host-and-deployment`. Required before any public contribution opening.

## Success criteria

License + governance files present and linked from README; OIDC reference verified against a test provider; headers present in prod config; anonymous write denial + self-approval denial re-verified; audit/scan gates green.
