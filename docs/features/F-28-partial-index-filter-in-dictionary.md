---
feature: F-28
epic: Foundation and identity
status: validating
board: 747
version: 1
---
# Partial index filter in the data dictionary

## Summary
The data dictionary that `tools/Simulab.DocGen` writes lists a partial index without its filter: since F-18, `docs/architecture/Jobs/data-dictionary.md` shows `ix_jobs_active_created_at` on `created_at`, but not `WHERE status IN (0, 1)`, so a reader takes it for a full index. Show the filter of every partial index (and the schema diagram too, if the format allows it). Captured at the F-18 retro (2026-09-23).

## Start
- Depends on: nothing. F-15 (DocGen), F-18 (the partial index itself), F-24 (descriptions), F-26 (the DBML
  schemas) and F-45 (the derived model list) are all `done` and on `main`.
- Waits on: nothing. It touches `tools/Simulab.DocGen/` and the generated documents only — no module, no schema.
- Suggested path: `/agile:autopilot F-28` — one renderer change, its tests, and the regenerated documents.
- Parallel with: anything. F-29 (`building`) and F-30 (`approved`) are both inside `Simulab.Identity`; the only
  overlap is the regenerated `docs/architecture/Identity/data-dictionary.md`, which neither of them rewrites.

## What exists (verified 2026-09-26, on `main` at `f598227`)
- The premise holds. `docs/architecture/Jobs/data-dictionary.md:22` and
  `docs/architecture/Identity/data-dictionary.md:99` both read `- \`ix_jobs_active_created_at\` on created_at`,
  with no sign of the filter. The index appears in **two** dictionaries because the `Job` entity is mapped into
  both module contexts; only those two have a `## jobs` section.
- The line comes from `EntityModels.RenderDictionary` (`tools/Simulab.DocGen/EntityModels.cs:284-292`): it orders
  `table.Table.Indexes` by name and writes `` - `name` on col, col `` plus ` (unique)` or
  ` (unique, NULLS NOT DISTINCT)`. Nothing reads the filter.
- **There is exactly one partial index in the solution today**: `ix_jobs_active_created_at`, declared with
  `.HasFilter(ActiveFilter)` in `src/BuildingBlocks/Simulab.Jobs/Persistence/Configurations/JobConfiguration.cs:45`
  and recorded as `status IN (0, 1)` in the migration snapshots of both contexts.
- **The filter never reaches the DBML schema, for a different reason.** `ix_jobs_active_created_at` covers
  `created_at`, a standard column F-26 leaves out of the diagram, so the whole index is dropped from
  `schema.dbml` — the filter is not missing there, the index is. No partial index is rendered in any `.dbml`
  today.
- DBML does support a note on an index (`created_at [name: 'created_at_index', note: 'Date']`), per the DBML
  documentation read on 2026-09-26, so a filter can be shown there when a partial index over visible columns
  appears.
- The unique flag and `NULLS NOT DISTINCT` are already shown, so the filter is the only thing missing from the
  line; the index method and the sort order are not shown either, and no index in the solution sets them.
- `DocGen --check` is part of the ship gate, so the regenerated documents are committed with the change or the
  gate goes red.

## Goal
A reader of the data dictionary can tell a partial index from a full one without opening the migrations: the
line says which rows the index covers.

## Users and use cases
- UC1 A developer reading a data dictionary sees which rows a partial index covers, on the index's own line, and
  stops mistaking it for a full index.
- UC2 A developer reading a DBML schema sees the same filter on a partial index the diagram shows, so the two
  documents never disagree.
- UC3 Whoever adds the next partial index gets it documented by the generator, without touching DocGen.

## Business rules
- BR1 A partial index in a data dictionary carries its filter at the end of its own line, as prose:
  `- \`ix_jobs_active_created_at\` on created_at, where status IN (0, 1)`.
- BR2 The filter joins what the line already says, inside the same parentheses when there are any:
  `- \`name\` on col (unique, NULLS NOT DISTINCT, where <filter>)`. There is never a second bracket.
- BR3 An index with no filter is written exactly as it is today: nothing is added, no empty `where`.
- BR4 The filter text is the one the model carries, unchanged — the same string the migration wrote
  (`status IN (0, 1)`). DocGen never reformats, re-cases or parses it.
- BR5 A partial index the DBML schema shows carries `note: 'where <filter>'` on its index line; an index the
  schema drops (because a column it covers is left out, F-26) stays dropped, filter or not.
- BR6 Nothing else joins the index line: not the method (btree, gin), not the sort order. No index in the
  solution sets either, and an undocumented default is not a defect.
- BR7 The generated documents are regenerated and committed with the change, so `DocGen --check` stays green.

## Screens and API
No screen, no route, no endpoint, no error code. The item changes `tools/Simulab.DocGen/EntityModels.cs` and the
documents it writes under `docs/architecture/`.

## Acceptance criteria
- AC1 Given the real Jobs model, when the data dictionary is rendered, then the `ix_jobs_active_created_at` line
  ends with `, where status IN (0, 1)` — and so does the Identity one, which maps the same table.
- AC2 Given an index with no filter, when the dictionary is rendered, then its line is byte for byte what it is
  today: no `where`, no empty parentheses.
- AC3 Given a unique index over a nullable tenant column that also has a filter, when the dictionary is rendered,
  then one bracket holds all three facts, in order: `(unique, NULLS NOT DISTINCT, where <filter>)`.
- AC4 Given a filter string with mixed case and spacing, when it is rendered, then it appears exactly as the model
  carries it.
- AC5 Given a partial index over columns the DBML schema shows, when the schema is rendered, then its index line
  carries `note: 'where <filter>'`; given a partial index the schema drops, then nothing about it appears.
- AC6 Given the repository, when `DocGen --check` runs after the change, then it passes — the two dictionaries were
  regenerated and committed.
- AC7 No new UI text: the localization criterion does not apply, and the missing-key test stays green.

## Decisions
- 2026-09-26 — the filter reads as prose, `, where <filter>`, inside the existing parentheses (owner) — the
  dictionary is prose and the line already carries `unique` and `NULLS NOT DISTINCT` that way; one way to read it.
- 2026-09-26 — the DBML schema carries the filter now, as `note: 'where ...'` (owner) — today it changes no file,
  because the only partial index covers a column F-26 drops; the test pins the behaviour before the case exists,
  instead of letting the next partial index lie in the diagram.
- 2026-09-26 — only the filter joins the line (owner) — no index in the solution sets a method or a sort order, so
  documenting them would be code with no real case.
- 2026-09-26 — AC3, AC4 and the DBML case of AC5 are tested over the test-only model that F-25 already built
  (`SharedTableDbContext`), since the real models have one partial index and it covers a hidden column — the same
  choice F-25 made for types sharing a table.
- 2026-09-26 — the filter is read through `index.MappedIndexes` and EF's own relational `GetFilter()`, the way the
  line already reads `GetAreNullsDistinct()` — no new abstraction.
- 2026-09-26 — no new package.
- 2026-09-26 — the merge stopped at the ship gate, twice, on a test this item does not touch:
  `Simulab.Jobs.Tests.JobWorkerTests.Start_WorkerDisabled_RunsNeitherTheJobsNorTheCleanup` (F-27). It passes 3/3
  alone and 3/3 with its whole project, and a clean `main` worktree failed once in three full runs on a different
  test (`MainLayoutTests.MenuButton_Desktop_TogglesCollapsedAndWritesCookie`), so the suite is not reliably green
  under parallel load and the cause is not here. Captured as `docs/bugs/B-19-full-suite-flaky-under-parallel-load.md`
  (AB#772). The item stays at `validating` until the owner decides; nothing was merged.
- 2026-09-26 — the owner followed the validation script and it passed: the two changed lines read as written, the
  DocGen filter gave `Passed! - Failed: 0, Passed: 72, Total: 72, Duration: 3 s`, `DocGen --check` gave
  `docs/architecture is up to date`, and `git diff main -- docs/architecture/Jobs/schema.dbml` was empty.
  Gate 2 is done; the merge is **not** authorized. The owner holds it until B-19 makes the full suite reliably
  green. The item stays at `validating` on `feature/F-28`; the worktree stays.

## Out of scope
- Index method, sort order, included columns, or any other index fact (BR6).
- The F-26 rule that drops an index covering a hidden column: it stays as it is.
- Any change to a module, a schema, a migration or a screen.

## Open questions
<!-- Approval is blocked while any line here is not answered or marked "deferred (owner, YYYY-MM-DD)". -->
- (none)

## Change notes
<!-- Added by /agile:change during build. Increase `version` in the header. -->
<!--
### v2 — YYYY-MM-DD
- What: <change>
- Why: <reason>
- Affected: <BR/AC ids>; other criteria unchanged.
- Re-approved: <YYYY-MM-DD>
-->

## Coverage
| Criterion | Test(s) |
|---|---|
| AC1 | `EntityModelsTests.RenderDictionary_SaysWhichRowsAPartialIndexCovers` — a case per module, `Jobs` and `Identity`, over the real models |
| AC2 | `EntityModelsTests.RenderDictionary_AddsNothingToAnIndexWithoutAFilter` — a plain index, a tenant-unique one, and no empty `where` anywhere |
| AC3 | `EntityModelsTests.RenderDictionary_PutsUniqueNullsAndTheFilterInOneBracket` — `(unique, NULLS NOT DISTINCT, where ...)`, brackets balanced |
| AC4 | the same test: the filter `ParentId  Is Not NULL` comes out with its own double space and casing |
| AC5 | `EntityModelsTests.RenderSchema_NotesTheFilterOfAPartialIndexItShows` (shown index, both single and composite) and `RenderSchema_LeavesOutAPartialIndexOverAColumnItDoesNotShow` (dropped index stays dropped) |
| AC6 | `dotnet run --project tools/Simulab.DocGen -- --check` → `docs/architecture is up to date`, run in both shells; the ship gate runs it again |
| AC7 | no UI text was added; the missing-key test is untouched and green in the full suite |

Seen failing first: with the renderer reverted to `main`'s version (`git stash push -- tools/Simulab.DocGen/EntityModels.cs`,
`git diff main` on that file empty), the run gave `Failed! - Failed: 4, Passed: 68, Total: 72` — the two AC1 cases,
AC3/AC4 and the AC5 note. The two that stayed green are the ones that assert today's behaviour (AC2 and the dropped
index), which is what they are for. Restored, then 135 of 135 green in the whole architecture project.

## Validation script
No screen: the item changes a generator and two generated lines.

1. Read the changed lines: `git diff main -- docs/architecture` in the worktree
   `D:\dev\_icontrol\wt\simulab\feature-28` → exactly two lines change, one in `Jobs/data-dictionary.md` and one in
   `Identity/data-dictionary.md`, both from ``- `ix_jobs_active_created_at` on created_at`` to
   ``- `ix_jobs_active_created_at` on created_at (where status IN (0, 1))``.
2. Run the DocGen tests.
   Git Bash: `dotnet test tests/Simulab.ArchitectureTests/Simulab.ArchitectureTests.csproj --nologo -v q --filter "FullyQualifiedName~DocGen"`
   PowerShell 7: `dotnet test tests\Simulab.ArchitectureTests\Simulab.ArchitectureTests.csproj --nologo -v q --filter "FullyQualifiedName~DocGen"`
   → `Passed! - Failed: 0, Passed: 72`. Both were run here and gave that.
3. Confirm the documents are in step with the code: `dotnet run --project tools/Simulab.DocGen -- --check` →
   `docs/architecture is up to date`.
4. Check the diagram is untouched: `git diff main -- docs/architecture/Jobs/schema.dbml` → empty, because that index
   covers `created_at`, which F-26 leaves out of the diagram. The DBML half of the change is held by a test over the
   F-25 test model instead.
5. See it bite, if you want: `git stash push -- tools/Simulab.DocGen/EntityModels.cs`, rerun step 2 → 4 failures
   naming the two dictionaries and the two shared-model lines; `git stash pop` and rerun → green.

## Delivery
<!-- Filled by /agile:ship. -->
- Branch: <feature/F-<number>>
- Merge: <commit>
- Tests: <count, duration>
- Manual pages: <paths>
