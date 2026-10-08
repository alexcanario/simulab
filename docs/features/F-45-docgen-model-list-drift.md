---
feature: F-45
epic: Foundation and identity
status: done
board: 768
version: 1
Autopilot: shipping
---
# The DocGen tests see every context DocGen documents

Technical terms: [glossary](../glossary.md)

## Summary
`EntityModelsTests.RealModels()` lists the contexts by hand, and it drifted: it never loaded `AiDbContext`, although DocGen has generated `docs/architecture/Ai/` since F-41, because DocGen discovers contexts from the assemblies next to it (`EntityModels.Load`). For four weeks the generator documented a table that every test built on that list was blind to. F-24 found it only because its new rule walks the same list and would have passed without ever looking at `ai_calls`. Make the two agree by construction: a test that compares what DocGen discovers with what the tests load, so a context added later cannot be documented and untested at the same time. Raised by the retro of F-24 (2026-09-25).

## Start
- Depends on: nothing. F-15 (DocGen) and F-24 (the description rules) are done, and both are on `main`.
- Waits on: nothing.
- Suggested path: `/agile:autopilot F-45` — small and well understood; the shape of the check is the only open choice.
- Parallel with: anything. It touches `tests/Simulab.ArchitectureTests/DocGen/` only.

## Goal
Make it impossible for DocGen to document a module's tables while every test built on `RealModels()` is blind to them. The rules that walk that list (F-24 descriptions, F-25, F-26) must see exactly what the generator writes.

## What already exists (verified 2026-09-26)
- `EntityModels.Load()` with no arguments (`tools/Simulab.DocGen/EntityModels.cs:26`) is DocGen's discovery: every `*.dll` next to the tool except the framework and the provider prefixes, then every non-abstract `DbContext` among their types. The tool has no list.
- `EntityModelsTests.RealModels()` (`tests/Simulab.ArchitectureTests/DocGen/EntityModelsTests.cs:16`) calls the overload that takes types, with four assemblies named by hand. `AiDbContext` is there since F-24, so the concrete gap is closed; the hand list is what F-45 removes.
- `Simulab.ArchitectureTests.csproj` references DocGen and every production assembly, **plus** `Simulab.Persistence.Tests` (sample context), and the test assembly itself declares `SharedTableDbContext` (F-25). So the test output folder holds more contexts than DocGen's: calling the parameterless `Load()` from the tests would discover contexts DocGen never documents. That option is out.
- `SolutionAssemblies.All` is the existing single list of production assemblies, pinned by `ForbiddenReferencesTests.The_rules_see_every_production_assembly`.
- `docs/architecture/` holds `Ai/`, `Catalog/`, `Identity/`, `Jobs/` (each with `schema.dbml` and `data-dictionary.md`), plus `System/routes.md`, `modules.md` and `README.md`. The gate runs `DocGen --check`, so those folders are never behind the code.
- DocGen's `docgen.json` has `entities` and `dataDictionary` both true.

## Users and use cases
- UC1 A developer adds a module with a `DbContext` and references it from DocGen; the architecture tests fail until that module is loaded by the tests too, so its tables are covered by the description rules from the first run.
- UC2 A developer reads the failure and it names the module, the direction of the drift and the file to edit — no hunting.

## Business rules
- BR1 The set of modules the DocGen tests load is exactly the set of modules DocGen documents under `docs/architecture/` (a folder holding `schema.dbml` or `data-dictionary.md`).
- BR2 The list of contexts the tests load is derived, not written by hand: it comes from DocGen's own project references (`tools/Simulab.DocGen/Simulab.DocGen.csproj`, transitively), resolved to assemblies.
- BR3 A project DocGen references that the tests cannot resolve to a loaded assembly fails the check, naming that project and `tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj` as the file to edit. It is never skipped silently.
- BR4 The comparison fails in both directions: a documented module with no model loaded, and a loaded model with no documented folder.
- BR5 The comparison only runs while `docgen.json` has `entities` or `dataDictionary` on; with both off DocGen writes no module folder and there is nothing to compare.
- BR6 The derived list never contains a context that is not DocGen's: the sample context of `Simulab.Persistence.Tests` and `SharedTableDbContext` stay out.

## Screens and API
None. No route, no endpoint, no UI text: the item only touches `tests/Simulab.ArchitectureTests/DocGen/`.

## Acceptance criteria
- AC1 Given the solution as it stands, when the DocGen tests build their context list from DocGen's project references, then the modules are exactly `Ai`, `Catalog`, `Identity`, `Jobs` and every test that used `RealModels()` still passes.
- AC2 Given the module folders under `docs/architecture/`, when they are compared with the loaded modules, then the two sets are equal; the test names any module present on only one side and which side.
- AC3 Given a documented module with no model loaded (a folder added to a copy of the doc set), when the comparison runs, then it fails naming that module and saying DocGen documents it while no test loads it. (Pure function over given inputs; this is the F-24 drift, reproduced.)
- AC4 Given a project DocGen references that resolves to no loaded assembly, when the list is derived, then the check fails naming that project and the tests `.csproj`.
- AC5 Given `docgen.json` with `entities` and `dataDictionary` both off, when the comparison runs, then it passes without expecting any module folder.
- AC6 No new UI text: the localization criterion does not apply, and the missing-key test stays green.

## Decisions
- 2026-09-26 — the truth for "what DocGen documents" is the committed `docs/architecture/` folder set, not DocGen's bin folder — the bins on disk can be stale (when this was written, the `tools/Simulab.DocGen/bin` of the main checkout held no `Simulab.Ai.dll`, four features after the reference was added), while `DocGen --check` in the gate keeps the committed docs in step with the code.
- 2026-09-26 — the derived list walks DocGen's project references transitively — DocGen's output folder also holds transitively referenced assemblies, so a context arriving through one of them would be documented; only the closure matches what the tool really sees.
- 2026-09-26 — a `System` folder (routes only) is not a module folder — the comparison counts a folder as a module only when it holds `schema.dbml` or `data-dictionary.md`.
- 2026-09-26 — the derived list resolves each referenced project against `SolutionAssemblies.All`, and an unresolved one throws `InvalidOperationException` naming the project and the tests `.csproj` — skipping it would put the tests back to being blind, quietly (BR3).
- 2026-09-26 — no independent review (`/agile:review`): the change is test-only, about 280 lines, and touches no authentication, data, contract or money — outside the `change-review` list.
- 2026-09-26 — the same drift one level up (`SolutionAssemblies.All`, a hand list pinned by a second hand list) was captured as `docs/features/F-46-derive-solution-assembly-list.md` (AB#770) and left out of this item — it reaches every architecture rule, not only DocGen.
- 2026-09-26 — the comparison logic is a pure function over (documented modules, loaded modules) and over (referenced projects, resolvable assemblies), so AC3 and AC4 are tested without touching the repository or breaking the build.

## Out of scope
- Changing DocGen's discovery itself, `docgen.json` or any generated document.
- Removing the hand-written expectation of `ForbiddenReferencesTests.The_rules_see_every_production_assembly`, or deriving `SolutionAssemblies.All`.
- Adding any module or table.

## Open questions
- (none)

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `DocGenModelDriftTests.TheDerivedList_NamesEveryModuleDocGenReferences`, `DocGenModelDriftTests.TheDerivedList_LeavesOutTheContextsDocGenNeverSees`, `EntityModelsTests.Load_NamesEveryRealModule` (and the 22 other tests that read `RealModels()`) |
| AC2 | `DocGenModelDriftTests.TheModulesOnDisk_AreTheModulesTheTestsLoad`, `DocGenModelDriftTests.TheModulesOnDisk_AreTheFoldersWithASchemaOrADictionary` |
| AC3 | `DocGenModelDriftTests.Drift_NamesADocumentedModuleNoTestLoads`, `DocGenModelDriftTests.Drift_NamesALoadedModuleWithNoGeneratedFolder`, `DocGenModelDriftTests.Drift_OnTheSameModules_IsEmpty` |
| AC4 | `DocGenModelDriftTests.Unresolved_NamesTheProjectAndTheFileToEdit`, `DocGenModelDriftTests.Unresolved_OnAClosureEveryAssemblyAnswersFor_IsEmpty`, `DocGenModelDriftTests.Closure_FollowsReferencesThroughAnotherProjectWithoutRepeating` |
| AC5 | `DocGenModelDriftTests.WithEntitiesAndTheDictionaryOff_NoModuleFolderIsExpected`, `DocGenModelDriftTests.WithEitherDocumentOn_TheModulesAreRead`, `DocGenModelDriftTests.TheCommittedOptions_KeepTheComparisonOn` |
| AC6 | No UI text was added; the missing-key test is unchanged and green in the full suite. |

The drift was seen failing against the real repository, not only through the pure function: with the
`Simulab.Ai` reference removed from `tools/Simulab.DocGen/Simulab.DocGen.csproj`,
`TheModulesOnDisk_AreTheModulesTheTestsLoad` failed with
`Ai: DocGen documents docs/architecture/Ai/ and no test loads its model — ... add the project to tools/Simulab.DocGen/Simulab.DocGen.csproj`,
while every other rule built on `RealModels()` stayed green — which is exactly how F-24's four weeks of blindness passed unnoticed. The reference was restored (`git diff tools/` is empty).

## Change notes

## Validation script
No screen, no app host: the item lives in the architecture tests. Two commands, both already run here.

1. In the item's worktree `D:\dev\_icontrol\wt\simulab\feature-45`, run the DocGen tests.
   Git Bash: `dotnet test tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj --nologo -v q --filter "FullyQualifiedName~DocGen"`
   PowerShell 7: `dotnet test tests\Simulab.ArchitectureTests\Simulab.ArchitectureTests.csproj --nologo -v q --filter "FullyQualifiedName~DocGen"`
   → `Passed! - Failed: 0, Passed: 66`.
2. Run the whole architecture project the same way without the filter → `Passed! - Failed: 0, Passed: 129`.
3. Confirm the generator agrees with the committed documents: `dotnet run --project tools/Simulab.DocGen -- --check` → `docs/architecture is up to date`.
4. Reproduce the guard if you want to see it bite: delete the `Simulab.Ai` line from `tools/Simulab.DocGen/Simulab.DocGen.csproj`, rerun step 1 → 2 failures naming `Ai` and the file to edit; restore the line with `git checkout tools/Simulab.DocGen/Simulab.DocGen.csproj` and rerun → green again.
5. Read `tests/Simulab.ArchitectureTests/DocGen/EntityModelsTests.cs` → `RealModels()` names no assembly by hand any more.

## Delivery
- Branch: `feature/F-45` (worktree `D:\dev\_icontrol\wt\simulab\feature-45`, removed at the merge)
- Merge: see the `--no-ff` merge of `feature/F-45` on `main` (AB#768)
- Tests: full suite green — 1331 tests, 0 failed, build 20 s, tests 68 s (`agile gate GREEN`, 0 warnings, baseline still 0 entries). The architecture project: 129 passed, of which 14 are new.
- Manual pages: none. Nothing a student or an administrator sees changed; the item lives in the architecture tests. `docs/infra.md` carries the new guard under "Technical docs" and the measured times.
