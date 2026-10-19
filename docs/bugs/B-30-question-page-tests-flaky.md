---
bug: B-30
feature: -
status: idea
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
