---
bug: B-20
feature: F-34
status: refining
board: 775
severity: low
---
# Exam scope column comment names values that do not exist

## What happens
1. `ExamConfiguration.cs:45` gives the `catalog.exams.scope` column the comment "How far the exam reaches: Federal, State, Municipal, National or International."
2. `ExamScope` has only `National`, `State` and `Municipal` (F-34 BR7).
3. The wrong comment reaches the database through migration `20260925171641_AddColumnDescriptions.cs:434` and the model snapshot (`CatalogModuleDbContextModelSnapshot.cs:97`), and the generated technical docs (`docs/architecture/Catalog/data-dictionary.md:22`).

Evidence: `grep -rn "National or International" --include=*.cs --include=*.md .` on `main` at 0f0d484 lists those four files. Found during the F-35 refinement on 2026-09-28; the owner asked to capture it.

## Start
- Depends on: nothing.
- Waits on: nothing.
- Suggested path: `/agile:autopilot B-20` (cause clear, fix small: the comment, a migration that changes it, `DocGen` regenerated). F-35 adds a migration to the same module, so either goes after the other.
- Parallel with: unknown — settled at /agile:refine.

## Expected

## Cause
Reproduced on `bug/B-20` at 0d86c7e (2026-09-29).

- `src/Modules/Catalog/Simulab.Catalog.Infrastructure/Persistence/Configurations/ExamConfiguration.cs:45` writes the column comment by hand, listing `Federal` and `International`, which `ExamScope` (`src/Modules/Catalog/Simulab.Catalog.Contracts/ExamScope.cs`: `National`, `State`, `Municipal`) does not have. The column stores the enum name (`HasConversion<string>()`), so the comment describes values the column can never hold.
- It reached the database through `20260925171641_AddColumnDescriptions.cs:434` (F-24), is copied into `20260925171641_AddColumnDescriptions.Designer.cs:100`, `20260928215100_AddExamEditions.Designer.cs:100` (F-35) and `CatalogModuleDbContextModelSnapshot.cs:97`, and into the generated `docs/architecture/Catalog/data-dictionary.md:52` (line 22 in the capture moved after F-35).
- Root cause: every enum column comment is a hand-typed copy of the enum's names, and nothing compares the two. F-24 wrote them all (commits f5c6ac7, 5be8b7f); F-34 then narrowed `ExamScope` and the comment was not updated.

Same defect elsewhere (a comment that lists or cites enum values the enum does not have):
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Persistence/Configurations/UserConfiguration.cs:32` — "PendingVerification, Active or Erased"; `AccountStatus` has `Pending`, not `PendingVerification`.
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Persistence/Configurations/RoleChangeConfiguration.cs:19` — "such as RoleCreated, RoleRenamed, ..."; `RoleChangeAction` has `RoleUpdated`, not `RoleRenamed`.
- `src/Modules/Identity/Simulab.Identity.Infrastructure/Persistence/Configurations/AccountEventConfiguration.cs:29` — "such as InvalidCredentials or LockedOut"; `AccountEventReason` has no `InvalidCredentials` (it has `UnknownAccount`, `WrongPassword`, ...).

Checked and correct: `ExamConfiguration.cs:39` (`AssessmentType`), `OrganizerConfiguration.cs:39` (`OrganizerKind`), `ExamEditionConfiguration.cs:64` (`ExamEditionStatus`), `AccountEventConfiguration.cs:25` and `:27` (`AccountEventTypes`, `AccountEventMethod`).

## Fix

## Regression test

## Open questions
- (none)

## Validation script

## Delivery
- Branch: bug/B-20
- Merge: <commit>
