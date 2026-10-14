---
feature: F-58
epic: Foundation and identity
status: cancelled
board: 97
version: 1
---
Duplicate of F-12 (2026-10-09): the gap is covered by `SolutionLayoutTests.EveryProjectOnDisk_IsListedInTheSolution`.
<!--
One file per feature. Save as: docs/features/F-<number>-<slug>.md
Status flow: idea -> refining -> approved -> building -> validating -> done
- idea: title, summary and start only (/agile:idea, /agile:epic).
- refining: sections below filled during /agile:refine.
- approved: set only after the product owner says "approve F-<number>" and Open questions is empty or deferred.
- building / validating / done: set by /agile:build and /agile:ship.
Remove these comments when the file leaves `idea`.
-->
# Every project on disk is in the solution

Technical terms: [glossary](../glossary.md)

## Summary
F-46 derives the assemblies the architecture rules see from the projects listed in `Simulab.slnx`. A `.csproj` under `src/` or `tools/` that exists on disk but is missing from the solution escapes every one of those rules. An architecture test checks that every such project is listed in `Simulab.slnx`. Raised while refining F-46 (2026-10-01).

## Start
- Depends on: F-46 (the rules derived from the solution).
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-58` (small and clear) — unknown, settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine`.

## Decisions
- 2026-10-04 — Closed as already covered, not built. The premise was false: F-12 (commit `59fa899`) added `SolutionLayoutTests.EveryProjectOnDisk_IsListedInTheSolution`, which lists every `.csproj` under `src/`, `tests/` and `tools/` and fails when one is missing from `Simulab.slnx`. Today all 37 projects on disk are listed and the test is green. A project outside those roots is not production (F-46, BR1), so no gap is left. Issue #97 closed as not planned. Owner's choice at `/agile:refine`.
