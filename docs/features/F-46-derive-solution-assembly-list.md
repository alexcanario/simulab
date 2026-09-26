---
feature: F-46
epic: Foundation and identity
status: idea
board: 770
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
# The architecture rules see every production assembly by construction

## Summary
`SolutionAssemblies.All` lists every production assembly by hand, and `ForbiddenReferencesTests.The_rules_see_every_production_assembly` pins it against a second list, also written by hand. Both have to be edited when a project is added, and neither is derived from `Simulab.slnx`: a new project silently escapes every rule built on `All` (forbidden references, table and column descriptions, the AI gateway boundary) while both lists agree with each other. This is the same class of drift F-45 removed from the DocGen tests, one level up. Derive the list from the projects in `Simulab.slnx` — or pin it against them — so a project that no rule sees fails the build. Raised while refining F-45 (2026-09-26).

## Start
- Depends on: F-45 (the same shape of check, smaller; its solution is the reference). Not blocking: F-45 touches only `tests/Simulab.ArchitectureTests/DocGen/`.
- Waits on: nothing.
- Suggested path: `/agile:refine` first — how to map a project in `Simulab.slnx` to a loaded assembly (and which projects are exempt: hosts, tools, test-only projects) is a real choice, not a detail.
- Parallel with: anything outside `tests/Simulab.ArchitectureTests/`.
