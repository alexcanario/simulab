---
feature: F-144
epic: none
status: idea
board: 197
version: 1
---
# Bring the full test suite back under its time budget

Technical terms: [glossary](../glossary.md)

## Summary
The three slowest test projects (QuestionBank, Identity, Catalog) grew 40 s to 45 s each since F-125, and the full suite went over the 5 min budget of `CLAUDE.md` on one run. Measure where the time goes, then cut it.

Measured at the ship of F-78 (2026-10-19, `docs/infra.md` "Full test suite", 4236 tests in 13 projects):

| Run | Test run | QuestionBank | Identity | Catalog |
|---|---|---|---|---|
| F-125 (2026-10-18) | 207 s | 2 m 56 s | 3 m 19 s | 3 m 20 s |
| F-78, best | 251 s | 3 m 39 s | 3 m 55 s | 4 m 4 s |
| F-78, last green | 360 s | 5 m 2 s | 5 m 27 s | 5 m 50 s |

The last run had the machine shared with other sessions. Cause not measured: the breakdown is part of this item.

Done means a measured breakdown of where the time goes (database container start, migrations, per-test setup) and a change that brings the full suite back under the budget with margin, with the numbers recorded in `docs/infra.md`.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing; the numbers of `gate.js ship` are the check.
- Model: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine`. Cause not verified: measure it first.
- Parallel with: B-30 (both touch the test setup, in different projects; keep them in sequence if they share the test base).
