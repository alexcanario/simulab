---
feature: F-46
epic: Foundation and identity
status: validating
board: 21
version: 1
---
# The architecture rules see every production assembly by construction

## Summary
`SolutionAssemblies.All` lists every production assembly by hand, and `ForbiddenReferencesTests.The_rules_see_every_production_assembly` pins it against a second list, also written by hand. Both have to be edited when a project is added, and neither is derived from `Simulab.slnx`: a new project silently escapes every rule built on `All` (forbidden references, table and column descriptions, the AI gateway boundary) while both lists agree with each other. This is the same class of drift F-45 removed from the DocGen tests, one level up. Derive the list from the projects in `Simulab.slnx` so a project that no rule sees fails the build. Raised while refining F-45 (2026-09-26).

## Start
- Depends on: F-45 (the same shape of check, smaller; its solution is the reference) — `done`.
- Waits on: nothing.
- Needed to validate: nothing beyond the test run; there is no screen.
- Suggested path: `/agile:build F-46`.
- Parallel with: anything outside `tests/Simulab.ArchitectureTests/` (F-42 and F-44 do not touch it).

## Goal
A project added to the solution is seen by every architecture rule without anyone remembering to edit a list; one that the rules cannot see fails the test run and says which line to add.

## What already exists (verified 2026-10-01)
- `tests/Simulab.ArchitectureTests/SolutionAssemblies.cs` — `All`, 22 assemblies written as `typeof(...).Assembly`, last edited by F-39 and F-41. Used by 8 test files: `ForbiddenReferencesTests`, `VocabularyTests`, `TableDescriptionTests`, `AiGatewayBoundaryTests`, `BuildingBlockBoundaryTests`, `DocGen/DocGenModelDriftTests`, `DocGen/EntityModelsTests`, `DocGen/DocGenProjectReferences` (in its message text).
- `ForbiddenReferencesTests.The_rules_see_every_production_assembly` — a second hand-written list of the same 22 names.
- `Simulab.ArchitectureTests.csproj` — a third hand-written list: 12 `ProjectReference` lines with the comment "Add each new module project here"; the other production assemblies arrive through the reference closure.
- `Simulab.slnx` lists 23 projects under `src/` and `tools/`. The one missing from `All` is `Simulab.AppHost`; nothing in the code says whether that was deliberate.
- No `.csproj` or `.props` sets `AssemblyName`: a project's file name is its assembly name.
- `SolutionLayoutTests` already reads `Simulab.slnx` (`ListedProjects`); `DocGenProjectReferences` (F-45) is the reference for a derived list with a message that names the file to change.

## Users and use cases
- UC1 A developer adds a project under `src/` or `tools/` and lists it in `Simulab.slnx`; the architecture rules see it with no list to edit, or the test run fails naming the `ProjectReference` to add.
- UC2 A developer exempts a project from the rules; the exemption is written once, with its reason, and is removed when the project leaves the solution.

## Business rules
- BR1 The production projects are every `Project` in `Simulab.slnx` whose path starts with `src/` or `tools/`, minus the exempt projects. Projects under `tests/` are never production.
- BR2 A project's assembly name is its `.csproj` file name without the extension.
- BR3 `SolutionAssemblies.All` is the production projects' assemblies, loaded by name; no hand-written list of production assembly names remains in the architecture tests.
- BR4 The exempt projects are one list in code, each entry with its reason. Today it holds only `Simulab.AppHost` (the Aspire orchestrator: referencing it would bring the `Aspire.Hosting` packages into the architecture tests).
- BR5 A production project whose assembly the test project cannot load fails the run with a message that names the project and the `ProjectReference` to add to `tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj`.
- BR6 An exempt project that is no longer in `Simulab.slnx` fails the run: an exemption never outlives its project.
- BR7 The `ProjectReference` lines of the architecture tests stay explicit; BR5 is what keeps them complete.

## Screens and API
- None. The change is inside `tests/Simulab.ArchitectureTests/`.

## Acceptance criteria
- AC1 Given today's `Simulab.slnx`, when the architecture tests run, then `SolutionAssemblies.All` holds exactly the assemblies of the projects under `src/` and `tools/` except `Simulab.AppHost` (22 today), derived from the file. (BR1, BR2, BR3, BR4)
- AC2 Given a solution that lists a production project whose assembly cannot be loaded, when the list is derived, then it fails with a message naming that project and the `ProjectReference` to add. (BR5, UC1)
- AC3 Given an exemption for a project the solution no longer lists, when the list is derived, then it fails naming the stale exemption. (BR6, UC2)
- AC4 Given a solution with projects under `tests/`, when the production projects are selected, then none of them is selected. (BR1)
- AC5 Given the derived list, when the existing rules run (forbidden references, vocabulary, table descriptions, AI gateway boundary, building block boundary, DocGen drift), then they are green and each still looked at something. (BR3)
- AC6 No UI text is added or changed, so no resource file in pt-BR, pt-PT or en changes.

## Decisions
- 2026-10-01 — Derive `All` from `Simulab.slnx` instead of pinning a hand-written `All` against it — removes both hand lists; the owner chose it.
- 2026-10-01 — Production = `src/` and `tools/` minus an exempt list; only `Simulab.AppHost` is exempt — `Simulab.DocGen` stays in on purpose (F-15), and the AppHost would pull `Aspire.Hosting` into the tests; the exemption makes today's silent gap explicit. Owner's choice.
- 2026-10-01 — The architecture tests' `ProjectReference` lines stay explicit, guarded by BR5 — easy to read; a glob would put every new project into the tests' build without anyone deciding. Owner's choice.
- 2026-10-01 — A `.csproj` on disk but missing from `Simulab.slnx` is out of scope, captured as F-58 (#97). Owner's choice.
- 2026-10-01 — Assemblies are loaded with `Assembly.Load(new AssemblyName(name))`; the pure part (select projects, check exemptions, report unresolved names) is a function over the solution text and the loadable names, tested without the real file, as F-45 did — technical choice, no product impact.
- 2026-10-01 — `DocGenProjectReferences.UnresolvedMessage` and the `.csproj` comment drop "and to SolutionAssemblies.All" / "Add each new module project here" in favour of the BR5 wording — the list they mention no longer exists.
- 2026-10-01 — The file header's `board: 770` was a leftover id; the GitHub issue is #21.
- 2026-10-01 — Approved by the owner ("aprovo F-46").
- 2026-10-01 — No new package: the tests already have xUnit, AwesomeAssertions and `System.Xml.Linq`.

## Out of scope
- A `.csproj` on disk that is not in `Simulab.slnx` — F-58.
- `SolutionLayoutTests.TestsRootProjects`, a hand list of test-only projects; it is not a production rule.
- Rules for `Simulab.AppHost` itself (its drift against the overview is already checked by `ArchitectureOverviewTests` from its source).

## Open questions
- (none)

## Change notes

## Validation script
Needed to validate: nothing beyond the test run (no screen, no app host).

1. Run `dotnet test tests/Simulab.ArchitectureTests` (Git Bash and PowerShell 7 alike) from the worktree: expect `Passed! - Failed: 0, Passed: 164`.
2. Read `tests/Simulab.ArchitectureTests/SolutionAssemblies.cs`: no `typeof(...)` list remains; `ExemptProjects` holds only `Simulab.AppHost`, with its reason.
3. Try UC1: add `<Project Path="src/BuildingBlocks/Simulab.Fake/Simulab.Fake.csproj" />` to the `/src/BuildingBlocks/` folder of `Simulab.slnx`, rerun step 1 with `--no-build`: the run fails with "The solution lists Simulab.Fake, which the architecture tests cannot load. Add a ProjectReference ..." (undo with `git checkout Simulab.slnx`). Checked on 2026-10-02.
4. Try UC2: add `["Simulab.Gone"] = "x"` to `ExemptProjects`, rerun: the run fails naming `Simulab.Gone` (undo with `git checkout tests/Simulab.ArchitectureTests/SolutionAssemblies.cs`).

## Coverage (criterion → test)
- AC1 → `ProductionProjectsTests.All_OnTheRealSolution_HoldsTheProductionProjectsExceptTheAppHost`, `Listed_TakesSrcAndToolsAndNamesThemByFile`, `Exempt_OnTheRealSolution_NamesEachProjectWithItsReason`, `ForbiddenReferencesTests.The_rules_see_every_production_project_of_the_solution`
- AC2 → `Unresolved_NamesTheProjectAndTheProjectReferenceToAdd`, `Unresolved_WithTheRealLoader_NamesAProjectNoOutputFolderHolds`
- AC3 → `StaleExemptions_NamesTheExemptionAndTheEntryToRemove`
- AC4 → `Listed_NeverTakesAProjectUnderTests`
- AC5 → the existing rules, unchanged and green (164 tests), each with its own presence assertion
- AC6 → no resource file touched

## Delivery
<!-- Filled by /agile:ship. -->
