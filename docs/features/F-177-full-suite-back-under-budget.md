---
feature: F-177
epic: test-infrastructure
status: idea
board: 245
version: 1
---
# Bring the full test suite back under its five-minute budget

Technical terms: [glossary](../glossary.md)

## Resume here
- Status: idea (2026-10-26)
- Stopped at: Captured from the F-166 retro, lesson 1; not refined.
- Next action: Owner decides whether to refine it (`/agile:refine F-177`) and whether the answer is to speed the slow projects or to revise the budget.
- Waiting on: nothing

## Summary
The full suite (`dotnet test Simulab.slnx`) has been over the five-minute budget of `CLAUDE.md` in four shipped items in a row. `docs/infra.md` records 370 s of test time at F-107, 384 s at F-110, 428 s at F-150 and 474 s at F-166 (5442 tests in 15 projects, 174 s over). In the F-166 run the three slowest projects ran in parallel and each took about 7.5 minutes: Identity 7 m 49 s, QuestionBank 7 m 37 s and Catalog 7 m 32 s. The new `Simulab.ExamEngine.Tests` adds 3 m 2 s for 70 tests, and the epic E-06 will add more host-based projects.

The item decides, with a measurement first, between speeding up the slow projects (shared host database per project, fewer containers, trimming the seed that F-110 added to every QuestionBank and Catalog host database) and revising the budget with the owner. Cause not verified: measure which tests and which host start-ups cost the time before proposing a fix.

Source: retro of F-166. The definition of done says an overrun is reported and becomes a retro finding; this is that finding.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing beyond a full `gate.js ship` run and its totals.
- Suggested path: `/agile:refine` → `/agile:build`.
- Parallel with: none that adds a host-based test project; B-32 (waits in bUnit tests) is a neighbour in the same epic.
