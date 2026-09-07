# AGENTS.md

## Project identity

Dharmatlas is an open historical atlas of Buddhism: a source-first, non-sectarian public knowledge infrastructure project. The MVP covers approximately 500 BCE–1000 CE in India, Central Asia, China, Sri Lanka, Tibet, Korea, and Japan.

## Non-negotiable product invariants

- Do not present one sectarian interpretation as uniquely authoritative.
- Do not state a material historical claim without a source or an explicit uncertainty label.
- Preserve traditional accounts separately from documented or scholarly historical claims.
- Support approximate and interval dates; never force false precision.
- AI output is draft assistance only. It may not invent citations or publish without human review.
- Keep the MVP focused on Timeline, Map, Person, Place, Text, Search, and Source.
- Do not add social, commercial, devotional, live, course, donation, fortune-telling, or full-canon-reader features without a separate approved change.

## Repository rules

- Use OpenSpec `spec-driven` changes for product work.
- Keep changes small and independently verifiable.
- Update the active change's `tasks.md` as work is completed.
- Preserve user changes and do not rewrite unrelated files.
- Use direct technical language in documentation and code comments.

## Required one-change workflow

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation.
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.

Incomplete or blocked work must not be claimed complete. For blocked work, record the exact failed command, relevant output, and the next action in `HANDOFF.md`.

## Validation

Run the applicable focused tests, then:

```bash
openspec validate --all --strict --no-interactive
git diff --check
git status --short
```

Do not archive or commit implementation changes while merely authoring documentation or a proposal.
