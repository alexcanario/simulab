---
bug: B-30
feature: -
status: refining
board: 196
severity: medium
---
# QuestionBank page tests time out under full-suite load

Technical terms: [glossary](../glossary.md)

## What happens
Two bUnit page tests of the QuestionBank area in `Simulab.Web.Tests` failed in 2 of the 4 full runs of the F-78 ship (2026-10-19), each time a different test, and passed alone and in the next full run:

1. Run 1 of `gate.js ship` in the worktree of F-78: `Simulab.Web.Tests.QuestionBank.QuestionsPageTests.Search_NothingMatches_NamesTheTerm` failed (`Failed: 1, Passed: 2138, Total: 2139`; the stack ends in `WaitForAssertionHelper` at `QuestionsPageTests.cs:169`, a `Markup.Should().Contain("No question matches")` check).
2. Run 3 (after the version bump): `Simulab.Web.Tests.QuestionBank.QuestionsFilterPageTests.ClearAll_RemovesEveryFilterAndTheSearch_AndHidesItself` failed (`Failed: 1, Passed: 2138, Total: 2139`).
3. `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~QuestionsPageTests"` on the same tree: `Passed: 21, Failed: 0`; with `FullyQualifiedName~QuestionBank` (the whole area): `Passed: 553, Failed: 0, Duration: 14 s`.
4. Runs 2 and 4 of the same tree were green (`Simulab.Web.Tests.dll`, 2139 of 2139). The branch only added a data migration, its tests and docs; it touches neither page.

Both tests wait with `WaitForAssertion` after `AdvanceDebounce()`, so the likely cause is the default bUnit wait timeout while the other test projects load the machine. Cause not verified: measure it at `/agile:refine`.

Already fixed by earlier bugs, so a later refinement does not read them as open: B-19 (the suite was not reliably green under parallel load; fixed with a thread-pool minimum in each project that starts a host) and B-22 (one data export dialog test). What is left: the bUnit wait of the QuestionBank page tests, which neither covered.

## Start
- Depends on: nothing. Related, both done: B-19, B-22.
- Waits on (to start): nothing.
- Needed to validate: nothing; the full suite, run several times, is the check.
- Suggested path: `/agile:refine` → `/agile:build`. Proposed fix, to confirm at refinement: raise the `WaitForAssertion` default timeout in one place (the shared test base) with a regression check; no new rule line, because `.claude/rules/agile/project.md:75` already asks for `WaitForAssertion` and the tests follow it.
- Parallel with: none.

## Cause
The premise of the report is false: the bUnit wait is already 5 s (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:43`, B-19) and the debounce already runs on a fake clock (`KitTestContext.cs:23`, F-48; `MudDebouncedInput` waits with `Task.Delay(interval, timeProvider)`, MudBlazor 9.9.0). A longer timeout cannot help a wait on a clock that only a test moves.

Reproduced on 2026-10-19 in this worktree (branch `bug/B-30` at `4ccba94`, debug build of `Simulab.Web.Tests`, 24 logical cores): 8 copies of the build output, each running `--filter "FullyQualifiedName~QuestionBank"`:
- 8 processes at once, 3 rounds: 24 runs, 24 green (`Passed: 553`, 13 s to 14 s);
- 24 processes at once, 2 rounds: 48 runs, 47 green and 1 red (33 s to 37 s each): `QuestionsPageTests.Search_SendsTheWordToTheServerAsSearch` (`QuestionsPageTests.cs:89-101`) after 7 s, `WaitForFailedException`, `Check count: 50`, and `Api.Received` holds the taxonomy call but no `search=crase` call. The wait ran for its whole 5 s and the search request was never made.

Mechanism (inferred, not yet proved by a diagnostic; the build proves it first): `Input(...)` returns before the text field's async handler has reached `DebounceDispatcher.DebounceAsync` (a bUnit event call can return before an async handler ran: B-11, measured). `AdvanceDebounce()` then moves the fake clock 300 ms before the delay exists, the delay starts later from the new "now", and nothing ever moves the clock again, so no search fires. This also explains why the two failures of F-78 were two different tests and why each passed alone: the window is a few scheduler ticks wide and only opens under load.

Duplicates: none that is a business rule (this is test timing). The same shape, `Input(...)` or a field change, then `AdvanceDebounce()` once, then a wait, is in 34 calls of `AdvanceDebounce` in 14 files (counts in parentheses): `Catalog/CatalogSearchPageTests.cs` (3), `ExamEditionFormTests.cs` (1), `ExamFormTests.cs` (1), `ExamsPageTests.cs` (2), `IssuingAuthoritiesPageTests.cs` (1), `OrganizersPageTests.cs` (1), `SubjectsPageTests.cs` (1), `QuestionBank/BaseTextsPageTests.cs` (2), `QuestionsFilterPageTests.cs` (3), `QuestionsPageTests.cs` (2), `Ui/AppDataTableTests.cs` (1), `AppLookupFieldTests.cs` (9), `AppSelectFieldTests.cs` (2), `AppSuggestFieldTests.cs` (3).

Callers: `KitTestContext.AdvanceDebounce` (`KitTestContext.cs:50`): the 34 calls above, each `same defect` (one advance right after the keystroke, never repeated); the rest of the suite does not call it.
