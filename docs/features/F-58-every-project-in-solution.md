---
feature: F-58
epic: Foundation and identity
status: idea
board: 97
version: 1
---
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

## Summary
F-46 derives the assemblies the architecture rules see from the projects listed in `Simulab.slnx`. A `.csproj` under `src/` or `tools/` that exists on disk but is missing from the solution escapes every one of those rules. An architecture test checks that every such project is listed in `Simulab.slnx`. Raised while refining F-46 (2026-10-01).

## Start
- Depends on: F-46 (the rules derived from the solution).
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-58` (small and clear) — unknown, settled at `/agile:refine`.
- Parallel with: unknown — settled at `/agile:refine`.
