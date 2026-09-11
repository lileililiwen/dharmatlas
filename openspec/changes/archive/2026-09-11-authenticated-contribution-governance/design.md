# Design: Authenticated Contribution Governance

## Identity boundary

Use an application-neutral authenticated principal mapped to a contributor record and role claims (`Contributor`, `Reviewer`, `Administrator`). Domain services receive verified actor identity from the host; callers cannot choose arbitrary reviewer IDs.

## Write flow

Typed request DTOs map to domain submissions. Validation checks JSON shape, source existence, target type/existence, ownership, and allowed transition before saving. Submission creation sets UTC timestamps server-side. Review approval runs in one transaction with a concurrency token and records decision, revision, and publication together.

## Safety and audit

Require reasons for decisions, retain actor/time/IP or request correlation metadata according to privacy policy, deny self-approval by default, and return safe validation errors without exposing private submissions.

## Verification

Test unauthorized access, ownership, role policy, malformed payloads, unknown references, self-approval, concurrent approvals, rollback, and audit completeness.
