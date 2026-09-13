# Security Policy

## Supported versions

Only the latest commit on the default branch is supported with security
updates. There are no long-term-support releases yet.

## Reporting a vulnerability

Email the maintainer contact configured for this repository
(`security@dharmatlas.example` unless deployment docs state otherwise).
Include:

- Affected component and version (commit SHA).
- Steps to reproduce or proof of concept.
- Impact assessment, if known.

We acknowledge receipt within 7 days. We target remediation or a workaround
within 90 days of a confirmed report, and we coordinate public disclosure
with the reporter. Do not open public issues for unpatched vulnerabilities.

## Scope

In scope: the host, public API, contribution/review paths, authentication
mapping, and container configuration in this repository.

Out of scope: private contributor data held by deployers, production IdP
operation, WAF/DDoS vendor behavior, and third-party infrastructure. There is
no bounty program; in particular, private-data exfiltration attempts are out
of scope and must not be tested against live deployments.

## Secrets

Never commit connection strings, OIDC client secrets, or private keys. The
host reads secrets from the environment and fails closed at startup when a
required key is missing. See `docs/operations.md` for the required variables.
