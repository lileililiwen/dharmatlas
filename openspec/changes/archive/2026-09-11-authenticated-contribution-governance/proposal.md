# Proposal: Authenticated Contribution Governance

## Why

The contribution domain has review states but no identity, role, ownership, source-reference existence checks, or concurrency boundary. Exposing it would allow impersonation and unsafe publication.

## What Changes

- Add provider-neutral contributor identity and persisted contributor roles mapped from authenticated subjects.
- Protect contribution submission, contributor history, and reviewer queue endpoints with authentication and role checks.
- Validate source and target references, server-owned timestamps, self-approval, relational transactions, optimistic concurrency, and moderation correlation metadata.

## Scope

Add authenticated contributor/reviewer roles, authorization policies, typed contribution requests, ownership checks, source/entity reference validation, correct timestamps, transactional approval, optimistic concurrency, and moderation audit metadata.

## Non-goals

- No public social profiles or reputation system.
- No automatic publication.
- No identity-provider vendor lock-in.
- No change to source-first or human-review invariants.

## Dependencies

Depends on `runtime-host-and-deployment`, `provenance-claims-and-evidence`, and `entity-name-persistence-and-read-model`.

## Success criteria

Only authorized users can submit or review, contributors cannot review their own work unless explicitly permitted by policy, invalid payloads never enter the queue, and concurrent approvals cannot corrupt published history.
