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

Added 2026-10-19 (retro of F-86): at the ship of F-86 (`gate.js ship`, 4 m 54 s total of which the test run was 294 s, 13 projects) the three slow projects ran Catalog 608 tests in 4 m 48 s, Identity 508 in 4 m 44 s and QuestionBank 456 in 4 m 5 s; every integration project is above the 2 min per project budget of the profile, and the full run is at the edge of the 5 min budget. Evidence: the output of that run (`agile gate GREEN`, per-project `Passed! ... Duration` lines).

Added 2026-10-21 (retro of F-89): at the ship of F-89 two full runs gave 343 s and 373 s of test run (14 projects, 4382 tests; Catalog 646 in 5 m 25 s to 6 m 6 s, Identity 508 in 5 m 32 s to 6 m 2 s, QuestionBank 456 in 4 m 15 s to 5 m 25 s); the machine was shared with other sessions. It is the third item in a row over the budget (B-30 307 s, F-78 360 s). Evidence: `docs/infra.md` "Full test suite".

Added 2026-10-21 (retro of F-84): at the ship of F-84 three full runs of nearly the same tree (14 projects, 4409 tests) gave 307 s, 276 s and 240 s of test run; the slow projects ran Catalog 646 in 4 m 22 s to 4 m 59 s, Identity 521 in 4 m 27 s to 4 m 57 s and QuestionBank 456 in 3 m 50 s to 4 m 16 s. The spread on one tree (67 s) shows how much the shared machine moves the result; the slowest project sets the wall time. It is the fourth item in a row at or over the budget. Evidence: `docs/infra.md` "Full test suite".

Added 2026-10-21 (retro of F-103): at the ship of F-103 the full run (`gate.js ship`, 14 projects, build 28 s, test run 459 s) gave QuestionBank 526 tests in 7 m 3 s, Identity 521 in 7 m 25 s and Catalog 646 in 7 m 32 s, with the shared machine busy. It is the fifth item in a row at or over the budget, and the worst run so far. Evidence: the output of that run (`tested Simulab.slnx (459 s)`, per-project `Passed! ... Duration` lines).

Done means a measured breakdown of where the time goes (database container start, migrations, per-test setup) and a change that brings the full suite back under the budget with margin, with the numbers recorded in `docs/infra.md`.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing; the numbers of `gate.js ship` are the check.
- Model: unknown — settled at /agile:refine.
- Suggested path: `/agile:refine`. Cause not verified: measure it first.
- Parallel with: B-30 (both touch the test setup, in different projects; keep them in sequence if they share the test base).
