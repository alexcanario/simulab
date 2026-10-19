---
bug: B-30
feature: -
status: done
board: 196
version: 2
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
- Needed to validate: nothing; no screen. The proof is the deterministic regression test plus the stress loop of `## Expected` (AC5).
- Model: sonnet (no signal: test-only change, one test project and the architecture tests, cause found)
- Suggested path: `/agile:build B-30` (the cause is found; the proposal of the first version of this file, a longer wait, is dropped: see `## Cause`).
- Parallel with: F-86 and F-89 (their worktrees do not touch the 14 test files; once this item merges, a test of theirs that still calls `AdvanceDebounce()` alone fails the new architecture test, so bring them up to date first).

## Cause
The premise of the report is false: the bUnit wait is already 5 s (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:43`, B-19) and the debounce already runs on a fake clock (`KitTestContext.cs:23`, F-48; `MudDebouncedInput` waits with `Task.Delay(interval, timeProvider)`, MudBlazor 9.9.0). A longer timeout cannot help a wait on a clock that only a test moves.

Reproduced on 2026-10-19 in this worktree (branch `bug/B-30` at `4ccba94`, debug build of `Simulab.Web.Tests`, 24 logical cores): 8 copies of the build output, each running `--filter "FullyQualifiedName~QuestionBank"`:
- 8 processes at once, 3 rounds: 24 runs, 24 green (`Passed: 553`, 13 s to 14 s);
- 24 processes at once, 2 rounds: 48 runs, 47 green and 1 red (33 s to 37 s each): `QuestionsPageTests.Search_SendsTheWordToTheServerAsSearch` (`QuestionsPageTests.cs:89-101`) after 7 s, `WaitForFailedException`, `Check count: 50`, and `Api.Received` holds the taxonomy call but no `search=crase` call. The wait ran for its whole 5 s and the search request was never made.

Mechanism (inferred, not yet proved by a diagnostic; the build proves it first): `Input(...)` returns before the text field's async handler has reached `DebounceDispatcher.DebounceAsync` (a bUnit event call can return before an async handler ran: B-11, measured). `AdvanceDebounce()` then moves the fake clock 300 ms before the delay exists, the delay starts later from the new "now", and nothing ever moves the clock again, so no search fires. This also explains why the two failures of F-78 were two different tests and why each passed alone: the window is a few scheduler ticks wide and only opens under load.

Duplicates: none that is a business rule (this is test timing). The same shape, `Input(...)` or a field change, then `AdvanceDebounce()` once, then a wait, is in 34 calls of `AdvanceDebounce` in 14 files (counts in parentheses): `Catalog/CatalogSearchPageTests.cs` (3), `ExamEditionFormTests.cs` (1), `ExamFormTests.cs` (1), `ExamsPageTests.cs` (2), `IssuingAuthoritiesPageTests.cs` (1), `OrganizersPageTests.cs` (1), `SubjectsPageTests.cs` (1), `QuestionBank/BaseTextsPageTests.cs` (2), `QuestionsFilterPageTests.cs` (3), `QuestionsPageTests.cs` (2), `Ui/AppDataTableTests.cs` (1), `AppLookupFieldTests.cs` (9), `AppSelectFieldTests.cs` (2), `AppSuggestFieldTests.cs` (3).

Callers: `KitTestContext.AdvanceDebounce` (`KitTestContext.cs:50`): the 34 calls above, each `same defect` (one advance right after the keystroke, never repeated); the rest of the suite does not call it.

## Expected
A bUnit test that types into a debounced field (the table search box, a lookup, a suggest or select field) gets its debounced result whether or not the field had registered its delay when the test moved the fake clock. Nothing under `src/` changes.

- AC1 Given a table with the search box and the fake clock moved before the field registered its delay (the window of `## Cause`, forced by calling `AdvanceDebounce()` before the keystroke), when the test types and waits with the new helper `WaitForAfterDebounce`, then the search request is made and the wait passes. The test is seen failing first with the old shape (one `AdvanceDebounce()`, then `WaitForAssertion`): it times out after 5 s.
- AC2 Given the helper, when its assertion does not pass on a try, then the next try moves the fake clock by the debounce interval again before it asserts; when the assertion passes, no further advance happens.
- AC3 Given the test sources under `tests/`, when the architecture test reads them, then no statement of a `*Tests.cs` file is `AdvanceDebounce(...)` alone: the 34 calls in the 14 files of `## Cause` all use the helper. The architecture test is seen failing on today's 34 calls before they are converted, and its finder has the same two self-tests as the B-23 guard (it names file and line; it leaves the helper alone).
- AC4 Given the converted tests, when `Simulab.Web.Tests` runs, then every test that used `AdvanceDebounce()` still passes with the same assertions (none is weakened or removed).
- AC5 Given 24 processes of `dotnet test --filter "FullyQualifiedName~QuestionBank"` at once on copies of the build output, 3 rounds, when they finish, then the result is recorded as measured: 69 of 72 green on 2026-10-19 (baseline: 47 of 48 of the same load). The 3 red are all `BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows`: two in round 1 (cold start of 24 hosts, `Check count: 0`) and one in round 3 (no search request despite 32 clock advances), captured as a new idea. The loop is weak evidence only; AC1, AC2 and AC3 are the proof.
- AC6 No file under `src/` changes, no package is added, and `KitTestContext.DefaultWaitTimeout` stays 5 s (`KitTestContext.cs:43`). No UI text: the localization criterion does not apply.

## Fix
- `KitTestContext`: add `WaitForAfterDebounce(IRenderedComponent, Action assertion, int milliseconds = AppLookupField.DebounceMilliseconds)`, which is `WaitForAssertion(() => { AdvanceDebounce(milliseconds); assertion(); })`: the clock is moved again on every try, so a delay registered late is still reached. `AdvanceDebounce` stays (the helper uses it).
- Convert the 34 calls of the 14 files of `## Cause`. A call whose next statement is not a `WaitForAssertion` is read case by case in the build (the assertion moves into the helper, or the call stays inside a lambda and the reason is a line in the test).
- `UiTestTimingTests` (`tests/Simulab.ArchitectureTests`): a second guard next to the B-23 one, with its finder and two self-tests.
- Deferred, recorded: the rule line "after typing into a debounced field wait with `WaitForAfterDebounce`" for `.claude/rules/agile/project.md` (UI tests) goes to the retro at the ship, with the owner's yes; the architecture test already holds the line.

## Docs
- none — test-only change; no technical page describes the debounce helper.

## Regression test

Tests locked: 7842397f822807387120894e678863e2dd7ca71d (2 files); unlocks: 0

- `DebounceWaitTests` (new, `tests/Hosts/Simulab.Web.Tests/Ui`): the window forced as AC1 says, the helper's retry as AC2 — Simulab.Web.Tests
- `UiTestTimingTests` (existing class, new guard `AdvanceDebounceAlone_InTestSources_IsNotFound` and its two self-tests `FindBareAdvanceDebounce_*`) — Simulab.ArchitectureTests
- Seen failing before the fix: `DebounceWaitTests` 3 of 3 (`NotImplementedException` from the skeleton); the guard lists 33 hits; the old shape times out (`WaitForFailedException`, 5 s).

## Decisions
- 2026-10-19 — The proposed fix of the report (a longer `WaitForAssertion` timeout) is dropped — it is already 5 s since B-19 and the debounce already runs on a fake clock since F-48; the reproduced failure waited the whole 5 s — verified in `KitTestContext.cs:23,43` and by the stress run in `## Cause` — asked
- 2026-10-19 — The 5 s ceiling stays — decided in F-48 (2026-10-03): a ceiling, a green run never waits on it — confirmed: docs/features/F-48-debounce-on-timeprovider.md
- 2026-10-19 — Scope: all 34 calls of `AdvanceDebounce` through one helper, plus an architecture guard — the same window is open in 11 more files; a guard stops a new test from bringing it back, as B-23 did for the URL read — asked
- 2026-10-19 — The fix is in the tests; the kit and `src/` are not touched — MudBlazor's debounce is correct, the window is in how a test moves the clock — Cause
- 2026-10-19 — The helper moves the clock on every try, not once: a fake clock cannot say whether a delay is registered (`FakeTimeProvider` has no pending-timer query), so retrying is the only way a test can be sure without sleeping — decided by Claude
- 2026-10-19 — The rule line for `project.md` goes through the retro, not this item — a project rule is the owner's to accept — decided by Claude
- 2026-10-19 — Main-session model: sonnet (no signal) — `references/model-choice.md` "Main session"
- 2026-10-19 — Count corrected at the build: the architecture guard finds 33 calls in 15 files, not 34 in 14 (the list of `## Cause` sums to 32; `Ui/AppTextFieldCounterTests.cs:94`, `AdvanceDebounce(500)`, was missing). It is converted like the rest; AC3 holds as written (no statement of the form is left) — verified by the red run of `AdvanceDebounceAlone_InTestSources_IsNotFound`
- 2026-10-19 — AC1's test forces the window with `Clock.Advance(...)` and not `AdvanceDebounce()`, so the new guard does not flag its own proof; the old shape (one `AdvanceDebounce()` then `WaitForAssertion`) was run once from a scratch test, not committed: `WaitForFailedException` after 5 s — decided by Claude
- 2026-10-19 — The helper loops itself and does not wrap one `WaitForAssertion`: bUnit 2.10.3 re-checks only when the component renders (measured: `Check count: 1` over 5 s with no render), so a single wait would retry only if something rendered; each try is `AdvanceDebounce` plus a 250 ms `WaitForAssertion` (a first stress run with 50 ms ended one test with `Check count: 0`: the window closed before the check started), until the 5 s ceiling — verified by the locked AC2 test, red on the first version (`Expected tries to be greater than or equal to 3, but found 1`)
- 2026-10-19 — plan — 1 skeleton `WaitForAfterDebounce` + `DebounceWaitTests` (AC1, AC2) + guard and self-tests (AC3) written red, committed and locked → AC1, AC2, AC3 red; 2 implement the helper → AC1, AC2 green; 3 convert the 33 calls of the 15 files, each assertion moves into the helper → AC3, AC4 green (`Simulab.Web.Tests` whole); 4 stress loop, 24 processes x 3 rounds on copies of the build output → AC5; 5 gate, scan, `git diff main --stat -- src` empty → AC6, validation script

- 2026-10-19 — Reuse: `KitTestContext.AdvanceDebounce` and bUnit's `WaitForAssertion` (the helper wraps both), the B-23 guard's file enumeration (extracted and shared by the new guard), the table's `RenderTable` shape from `AppDataTableTests` — decided by Claude
- 2026-10-19 — Shortcuts: none — decided by Claude
- 2026-10-19 — The 3 failures of the stress loop are captured as B-31 (board 198), not fixed here — AC5 changed to the measured result by change note v2 — asked

## Change notes
### v2 — 2026-10-19
- What: AC5 no longer asks for 72 of 72 green runs; it records the measured result, 69 of 72, and says the loop is weak evidence.
- Why: the stress loop after the fix gave 69 of 72 (baseline 47 of 48). All 3 red are one test, `BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows`: two cold-start starvation in round 1 (`Check count: 0`), one in round 3 with no search request despite 32 clock advances. AC1, AC2 and AC3 stay the proof.
- Affected: AC5 and step 5 of the validation script (both languages). Not affected: AC1-AC4, AC6, the code and the locked tests.
- Follow-up: the 3 failures are captured as a new idea.
- Re-approved by the owner: 2026-10-19 ("aprovo").

## Out of scope
- The slow projects of the full suite (QuestionBank, Identity and Catalog at 5 m or more on 2026-10-19, over the 5 m budget): already watched in `docs/infra.md` (Measured times); not caused by this window.
- The MudBlazor debounce itself and any change under `src/`.

## Open questions
- (none)

## Validation script
Needed to validate: nothing. No screen is involved. Close the IDE and any app host first (a running one locks the build output). Commands are the same in Git Bash and PowerShell 7, run from the worktree `D:\wt\simulab\b-30-question-page-tests`.

### English
1. [ ] Run the new tests: `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~DebounceWaitTests"` → `Passed!` with the tests of AC1 and AC2, none failed.
2. [ ] Run the architecture guard: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~UiTestTimingTests"` → `Passed!`, none failed.
3. [ ] Run the QuestionBank tests: `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~QuestionBank"` → `Passed!  - Failed: 0, Passed: 553` (plus the new tests of the area, if any).
4. [ ] Read the diff: `git diff main --stat -- src` → empty (nothing under `src/` changed).
5. [ ] Read the stress loop's real output under `## Delivery` (72 runs, 69 green, the 3 red named); the loop takes about 6 minutes and is not meant to be repeated by hand.

### pt-BR
1. [ ] Rode os testes novos: `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~DebounceWaitTests"` → `Passed!` com os testes dos AC1 e AC2, nenhum falhou.
2. [ ] Rode a trava de arquitetura: `dotnet test tests/Simulab.ArchitectureTests --filter "FullyQualifiedName~UiTestTimingTests"` → `Passed!`, nenhum falhou.
3. [ ] Rode os testes do banco de questões: `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~QuestionBank"` → `Passed!  - Failed: 0, Passed: 553` (mais os testes novos da área, se houver).
4. [ ] Leia o diff: `git diff main --stat -- src` → vazio (nada em `src/` mudou).
5. [ ] Leia a saída real do laço de estresse em `## Delivery` (72 execuções, 69 verdes, as 3 vermelhas nomeadas); o laço leva cerca de 6 minutos e não é para repetir à mão.

## Events

- 2026-10-19 question — asked 1, from context 1
- 2026-10-19 change — v2: AC5 now records the measured 69 of 72 stress runs; the loop is weak evidence

## Delivery
- Branch: bug/B-30
- Build state (2026-10-19): helper, 33 conversions and the guard done; `gate.js stop` ends `agile gate GREEN`; `Simulab.Web.Tests` 2142 of 2142; `scan.js run` ends `agile scan GREEN`; `git diff main --stat -- src` empty.
- AC5 stress loop, real output (24 processes at once, 3 rounds, copies of the build output inside the worktree): 69 of 72 green (`Passed: 553`), 3 red, all in `BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows`: two in round 1 (cold start of 24 hosts, 9 s and 10 s, `Check count: 0`: the machine starved the check itself) and one in round 3 (6 s, `Check count: 32`, the fake API held only the first list call: no search request came out despite 32 clock advances). AC5 as written (72 of 72) is not met; baseline 47 of 48.
Gate time: 5 passes, 10 min 24 s — turn 2 (53 s), ship 1 (5 min 42 s), scan 2 (3 min 49 s); 0 red
Usage: 2263.8k weighted (estimate) — refine 27% · build 41% · ship 9% · other 23% — context per turn 171k → 131k — 3 sessions, 133 turns
- Merge commit a95939d on main (version 1.5.0 -> 1.5.1); pushed; branch and worktree removed.
- Ship gate (2026-10-19): `agile gate GREEN`, 4242 tests in 13 projects, 0 failures, 35 s build, 307 s test run (slowest projects QuestionBank 4 m 14 s, Catalog 5 m, Identity 5 m); `agile scan GREEN`; `agile docs GREEN`; test lock unchanged.
- Manual: no page changed (test-only change, no visible behavior).
- Validation evidence: validated by the owner in chat on 2026-10-19; the script has no tick (the five steps are `not tested` in the tick file, 0 of 5 ticked).
