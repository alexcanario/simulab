---
bug: B-20
feature: F-34
status: validating
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
- Suggested path: `/agile:build B-20`. F-35, which added a migration to the same module, is merged (0d547e2), so nothing is in the way.
- Parallel with: any item that adds no migration to the Catalog or Identity module. F-36 and F-37 are still `idea`; if one of them reaches build first with a Catalog migration, do them in sequence.
- Needed to validate: nothing beyond the test containers; no screen.

## Expected
Every column comment that names values of an enum stored as text names only values the enum has. For the four columns in `## Cause`:

| Column | Comment after the fix |
| --- | --- |
| `catalog.exams.scope` | How far the exam reaches: National, State or Municipal. |
| `identity.users.status` | Where the account stands: Pending, Active or Erased. |
| `identity.role_changes.action` | What was done: RoleCreated, RoleUpdated, RoleDeleted or UserRolesChanged. |
| `identity.account_events.reason` | Why it failed, such as WrongPassword or LockedOut. Null when nothing failed. |

The database, the model snapshots and `docs/architecture/*/data-dictionary.md` say the same.

## Acceptance criteria
- AC1 (BR1) — Given the Catalog and Identity models, when the enum-comment test runs, then every enum value a comment names exists in that column's enum, and the test fails on `main` before the fix (it lists the four columns of `## Cause`).
- AC2 (BR2) — Given the new migrations applied to a test container, when `col_description` is read for the four columns, then it returns the texts of `## Expected`.
- AC3 (BR3) — Given the fix, when `DocGen --check` runs, then it passes and the two data dictionaries show the new texts.
- AC4 (BR4) — Given the migrations, when they run, then only comments change: no column type, constraint, index or row changes (the migration body is `AlterColumn` with comment and `oldComment` only).
- Localization: not applicable — no UI text changes; column comments are English repository text.

## Rules
- BR1 — A comment on a column that stores an enum as text may name that enum's values; every value it names must exist in the enum. A full list ("A, B or C") must name all of them; a partial list says "such as".
- BR2 — The fix reaches the database through one EF migration per module (Catalog, Identity), generated, not hand-written.
- BR3 — The generated technical docs are regenerated with `DocGen`, never edited by hand.
- BR4 — Only descriptions change; no data or schema shape changes.

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
1. Correct the four `HasComment` texts in `ExamConfiguration.cs`, `UserConfiguration.cs`, `RoleChangeConfiguration.cs` and `AccountEventConfiguration.cs` to the texts of `## Expected`.
2. Generate one migration per module (`FixEnumColumnDescriptions`) with `dotnet ef migrations add`; commit it with its designer file and the updated snapshot.
3. Regenerate `docs/architecture/` with `DocGen` and commit the result.

## Regression test
`tests/Simulab.ArchitectureTests/EnumColumnCommentTests.cs`, beside `TableDescriptionTests` and on the same real models (`EntityModelsTests.RealModels()`): for every property whose CLR type is an enum (nullable included) and whose provider type is `string`, take the comment, find the value list after the first `:` or after "such as", split it on `,` and ` or `, and require every PascalCase word there to be a name of that enum. When the list is not introduced by "such as", it must also name every value of the enum. Seen failing on `main` before step 1, listing the four columns (AC1). AC2 extends `ColumnCommentTests`-style reading of `col_description` to the two module test containers, or asserts on the migration's `AlterColumn` operations if a container read is already covered by the migration tests; the build decides and records it.

## Decisions
- 2026-09-29 — Fix all four columns in this bug, not only `catalog.exams.scope` (owner). Reason: same defect, same size of work, and the new test would flag the three Identity columns anyway.
- 2026-09-29 — The regression test checks the EF model for every enum column in every module, instead of one exact-string test or comments built from `Enum.GetNames` (owner). Reason: it catches the next drift in any module without changing how the nine configurations are written, and it also covers partial "such as" lists.
- 2026-09-29 — `role_changes.action` becomes a full list (four values) instead of "such as", since the enum has only four (technical). Reason: a full list is checked for completeness by BR1.
- 2026-09-29 — The table comment of `role_changes` ("created, renamed, deleted, granted, revoked") describes the actions in plain words, not enum names, and stays as it is (technical). Reason: renaming a role is a real `RoleUpdated` case; it names no value that does not exist.

## Out of scope
- Comments that describe free text or examples of data ("such as TRF1 or INSS") — they are not enum values.
- Changing how comments are written (generated from the enum) — rejected in `## Decisions`.

## Open questions
- (none)

## Validation script
Needed to validate: nothing beyond the test containers (Docker running); no screen.

1. In `D:\dev\_icontrol\wt\simulab\b-20-exam-scope-column`, run the new tests. Expected: `Passed! - Failed: 0` for each.
   - Git Bash and PowerShell 7: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~EnumColumnCommentTests"`
   - Git Bash and PowerShell 7: `dotnet test tests/Modules/Identity/Simulab.Identity.Tests --filter "FullyQualifiedName~FixEnumColumn"` (3 tests) and the same with `tests/Modules/Catalog/Simulab.Catalog.Tests` (1 test).
2. Read `docs/architecture/Catalog/data-dictionary.md` (`scope`) and `docs/architecture/Identity/data-dictionary.md` (`status`, `action`, `reason`): they show the texts of `## Expected`.
3. Check the generated docs are in sync: `dotnet run --project tools/Simulab.DocGen -- --check`. Expected: exit code 0.

## Delivery
- Change: the four `HasComment` texts, migration `FixEnumColumnDescriptions` in Catalog and in Identity (comment-only `AlterColumn`), regenerated `docs/architecture/`.

- Branch: bug/B-20
- Merge: <commit>
