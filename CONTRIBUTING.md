# Contributing to Dharmatlas

Dharmatlas is a source-first, non-sectarian public knowledge infrastructure
project. All product work follows the OpenSpec one-change workflow described
in `AGENTS.md`:

1. Select one active change with `openspec list`.
2. Implement only that change and its tests.
3. Update its `tasks.md` as tasks complete.
4. Verify with relevant tests and strict OpenSpec validation
   (`openspec validate --all --strict --no-interactive`, `git diff --check`).
5. Archive the completed change.
6. Commit 1: implementation, tests, archive, and related generated specs only.
7. Update `HANDOFF.md` with completion evidence and the next change.
8. Commit 2: only the `HANDOFF.md` update.
9. Stop; do not start another change or push.

## Developer Certificate of Origin (DCO)

All commits must include a `Signed-off-by` trailer, asserting the
[Developer Certificate of Origin](https://developerdiligence.com/issues/23/):

```
Signed-off-by: Your Name <you@example.com>
```

Use `git commit -s` to add the trailer. No separate CLA is required.

## Licensing of contributions

- Code contributions are licensed under the repository `LICENSE` (MIT).
- Data and seed contributions (entities, names, claims, sources) are released
  under [CC-BY-4.0](https://creativecommons.org/licenses/by/4.0/) as recorded
  in `data/seed/v2/manifest.json` (`metadata.license` field). Do not submit data you do
  not have the right to release under CC-BY-4.0.
- Every material historical claim in a contribution must cite a source or
  carry an explicit uncertainty label. AI output is draft assistance only and
  must pass human review before publication.

## Conduct

Participation is governed by `CODE_OF_CONDUCT.md`. Reports go to the contact
listed in `SECURITY.md`.

## Security

Do not open public issues for vulnerabilities. Follow `SECURITY.md`.
