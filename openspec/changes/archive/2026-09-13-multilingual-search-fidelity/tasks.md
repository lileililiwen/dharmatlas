# Tasks

- [x] Implement pure `NameNormalizer` (NFKC, IAST/Pinyin/Wylie folding, CJK bigrams) with vector tests.
- [x] Add migration for normalized columns + trigram/GIN indexes and ranking pipeline with `matchedName`/`matchKind`.
- [x] Author `multilingual.json` fixture (6 scripts × 4 romanizations) and benchmark runner with p50/p95 output.
- [x] Document supported scripts/schemes and adoption gate in `docs/search-fidelity.md`.
- [x] Verify 100% fixture pass, index selectivity on Postgres, caps hold under adversarial input.
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
