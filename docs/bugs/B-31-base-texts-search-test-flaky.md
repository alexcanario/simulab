---
bug: B-31
feature: F-102
status: refining
board: 198
severity: low
epic: Question bank
---
# Base texts search test still fails under heavy parallel load

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Web.Tests.QuestionBank.BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows` still failed in 3 of 72 runs of the stress loop of B-30 (2026-10-19: 24 `dotnet test --filter "FullyQualifiedName~QuestionBank"` processes at once, 3 rounds, on copies of the build output kept inside the worktree), all of them after the B-30 fix (`WaitForAfterDebounce`):
1. Round 1, twice (9 s and 10 s tests, `Check count: 0`, 228 and 268 renders): the bUnit wait ended before its check ran once; the cold start of 24 hosts starved the machine.
2. Round 3, once (6 s, `Check count: 32`): the fake API held only the first list call (`?page=0&pageSize=25`); no `search=tarifas` request came out although the helper moved the fake clock 32 times. The keystroke or its debounce was lost, not late: a different failure from the one B-30 fixed.

Already fixed by earlier bugs, so a later refinement does not read them as open: B-19 (thread-pool minimum), B-22 (one dialog test), B-23 (URL read after a click) and B-30 (a debounced wait whose clock advance came before the delay existed). What is left: the failure above, seen only in this test and only at 24 processes at once.

## Start
- Depends on: nothing. B-30 (done, merged 2026-10-19) added `WaitForAfterDebounce`, which this item changes.
- Waits on (to start): nothing.
- Needed to validate: nothing; no screen. The proof is the deterministic regression tests plus a stress run like the one in `docs/bugs/B-30-question-page-tests-flaky.md` (`## Delivery`).
- Model: sonnet (no signal: test-only change, one test project, cause measured)
- Suggested path: `/agile:build B-31` (the cause is measured; the premise of the report, a lost keystroke, is dropped: see `## Cause`).
- Parallel with: F-89 (its worktree touches no test helper; a conflict on `KitTestContext.cs` is possible but small). B-32 (idea) waits on this item.

## Expected
A bUnit test that types into a debounced field gets its result even when the machine stalls the test process for seconds (a cold start of many test hosts at once). Only the wait helper of the debounced fields changes: it gets a ceiling of its own, longer than the default one. Nothing under `src/` changes.

- AC1 Given a context whose default wait is 500 ms and a debounced wait whose assertion becomes true only after 1.2 s of wall time, when the test waits with `WaitForAfterDebounce`, then the wait passes (the helper uses `DebounceWaitTimeout`, not `DefaultWaitTimeout`). Seen failing first: with today's helper the same wait throws `WaitForFailedException` at about 500 ms.
- AC2 Given a context whose `DebounceWaitTimeout` is 1.5 s and an assertion that never passes, when the test waits with `WaitForAfterDebounce`, then it throws `WaitForFailedException` after 1.5 s or more and before 4 s (the ceiling is a maximum for a failing test, and the helper keeps moving the fake clock until then).
- AC3 Given the shared test context, when its settings are read, then `DebounceWaitTimeout` is 15 s and `DefaultWaitTimeout` stays 5 s (`KitTestContext.cs:46`, the F-48 decision).
- AC4 Given the helper changed, when `Simulab.Web.Tests` runs, then every test that waits with `WaitForAfterDebounce` (the 33 calls of B-30) and the two tests of B-30's `DebounceWaitTests` still pass with the same assertions (none is weakened or removed).
- AC5 Given 24 processes of `dotnet test --filter "FullyQualifiedName~QuestionBank"` at once on copies of the build output, 3 rounds, when they finish, then the result is recorded as measured, with the shape of each red (baseline of 2026-10-20, run 1 of `## Cause`: 13 reds of the first test of the process in 72 processes). The loop is weak evidence only and does not gate the ship: its load stalls processes for up to 10 s; AC1 to AC3 are the proof.
- AC6 No file under `src/` changes, no package is added, `KitTestContext.DefaultWaitTimeout` stays 5 s. No UI text: the localization criterion does not apply.

## Cause
The premise of the report is false: the keystroke is not lost. The failure is the whole test process being starved for seconds, and `WaitForAfterDebounce` gives up at its 5 s ceiling (`KitTestContext.cs:73`, `elapsed < DefaultWaitTimeout`, `DefaultWaitTimeout` = 5 s at `KitTestContext.cs:46`) while the request is still on its way.

Measured on 2026-10-20 in this worktree (branch `bug/B-31` from `main` at `fecd1a9`, debug build, 24 logical cores, MudBlazor 9.9.0). A scratch test (deleted, never committed) repeated the steps of `BaseTextsPageTests.Search_SendsTheWordToTheServerAsSearch_AndNarrowsTheRows` 15 times per process and logged the field's `DebounceDispatcher.IsPending`, `Text`, `Value` and the fake API's calls at each try. Load: 24 processes of `--filter "FullyQualifiedName~QuestionBank"` at once on copies of the build output, as in B-30.
- Run 1, deadline 6 s, 3 rounds (72 processes, 1080 diagnostic runs): 13 failures, all of the first test of the process (`run: 9`; cold start), none of the other 14. In all 13 the log reads the same: `pending=True` right after the input, `value='tarifas'` and `pending=False` at the first try (the debounce fired), a gap of 1.9 s to 9.7 s between the input and the first check, and the search request present in 8 of 13 (the other 5 reached their first try at 6.5 s or later). Never `pending=False` with an empty value, which is what a lost keystroke would show.
- Run 2, deadline 20 s, 2 rounds (48 processes, 720 diagnostic runs): 719 green, 1 red. The red one: input at 9.4 s, first check at 19.1 s, the request made at once after it (`calls=2`). That run was heavier than run 1 (the Stop hook of this session built the solution during it): 44 real tests of the area failed in the same 48 processes (`BaseTextsPageTests.Search_Sends...` 24 times, `QuestionsPageTests.Search_SendsTheWordToTheServerAsSearch` 11, `QuestionsFilterPageTests` 6, `QuestionOriginTests` 2, `QuestionsPageTests.Search_NothingMatches_NamesTheTerm` 1), each after 3 s to 19 s: the load, not one test, is the cause.
- Why this test: it is the first debounced search a process runs (JIT of MudBlazor's grid, the typed client and the fake API), so under a cold start of 24 hosts it takes the longest. The same shape explains the three failures of B-30's loop: `Check count: 0` (the check itself did not start inside the 5 s) and `Check count: 32` (32 clock advances, but the continuation after the debounce, the table reload and the HTTP call, did not get a thread in time).

Library reading (MudBlazor 9.9.0, raw `ilspycmd`): `DebounceDispatcher.DebounceAsync` takes a `SemaphoreSlim` and hops threads (`ConfigureAwait(false)`) before `Task.Delay(interval, timeProvider, token)`; `MudDebouncedInput.OnDebouncedUpdate` then runs `InvokeAsync` into the table reload (`MudDataGrid.InvokeServerLoadFunc` -> `AppDataTable.LoadServerDataAsync` -> `BaseTexts.LoadAsync` -> `QuestionBankApiClient`). Every step needs a thread-pool thread; none can drop the call. Nothing under `src/` is at fault.

Duplicates: none that is a business rule (this is test timing). Other tests that wait with `WaitForAfterDebounce` share the ceiling: 33 calls in 15 files (B-30), so a change of the helper's ceiling reaches them all. Tests that wait with a bare `WaitForAssertion` after a non-debounced action (for instance `Filter_ByLanguage_SendsTheTagAndNarrowsTheRows`) have the same 5 s default and fail the same way under the same load (`QuestionOriginTests.Save_WithAnOrigin_SendsTheEditionTheNumberAndTheNoticeSubject`, 2 times in run 2); they are out of this item.

Callers: `KitTestContext.WaitForAfterDebounce` (`KitTestContext.cs:61`): the 33 calls of B-30, each `same defect` (the ceiling is shared); no caller relies on the 5 s.

## Fix
- `KitTestContext` (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs`): add `protected virtual TimeSpan DebounceWaitTimeout => TimeSpan.FromSeconds(15)` and make `WaitForAfterDebounce` give up after it instead of after `DefaultWaitTimeout` (`KitTestContext.cs:73`). The doc comment of the helper says why: a stalled process can reach its first check seconds after the keystroke (measured: up to 9.7 s), and a green run never waits on a ceiling. `DefaultWaitTimeout` stays 5 s.
- Nothing else: the 33 callers need no edit, the try window (250 ms) and the clock advance on every try stay as B-30 left them.
- Deferred, recorded: the plain waits (no debounce) that fail under the same load are B-32 (idea, epic Test infrastructure); `.claude/rules/agile/project.md` line 76 stays as it is (it describes the helper's advance, still true).

## Docs
- none — test-only change; no technical page describes the debounce helper.

## Regression test
- `DebounceWaitTimeoutTests` (new, `tests/Hosts/Simulab.Web.Tests/Ui`): AC1 and AC2 with a derived context that sets `DefaultWaitTimeout` to 500 ms and overrides `DebounceWaitTimeout`; AC3 reads both settings — Simulab.Web.Tests

## Decisions
- 2026-10-19 — Captured as a separate bug and not as part of B-30 — B-30 was accepted with 69 of 72 runs and fixes the late-registered delay; this failure has another shape — asked
- 2026-10-20 — The premise of the report (the keystroke or its debounce was lost) is dropped — 1800 diagnostic runs under the B-30 load (24 processes, `IsPending`, `Text`, `Value` and the fake API's calls logged at every try) never showed a lost keystroke; every red was the process stalled for seconds and the request arrived right after the helper gave up — measured, see `## Cause` — asked
- 2026-10-20 — The 5 s default ceiling (`DefaultWaitTimeout`) stays, as decided in F-48 (2026-10-03: a ceiling, a green run never waits on it) — Yes, still true here — confirmed: docs/features/F-48-debounce-on-timeprovider.md
- 2026-10-20 — The helper gets a ceiling of its own, 15 s, rather than raising the default for every wait, closing with no change, or warming the process up — the measured stalls reached 9.7 s, 20 s still lost 1 of 720 under a heavier load, and only failing tests pay the longer wait — asked
- 2026-10-20 — The plain waits that fail under the same load are not folded in: captured as B-32 (board 200) in the new epic Test infrastructure (board 199, `docs/epics/test-infrastructure.md`) — asked
- 2026-10-20 — `DebounceWaitTimeout` is `protected virtual` so a test can shrink it and prove the ceiling in under 2 s, without sleeping 15 s — decided by Claude
- 2026-10-20 — The stress loop (AC5) stays a recorded measurement, not a gate: its load is artificial (24 processes on 24 cores, plus the Stop hook's own build during run 2) and stalls processes beyond any fixed ceiling — decided by Claude
- 2026-10-20 — Main-session model: sonnet (no signal) — `references/model-choice.md` "Main session"

## Out of scope
- The plain `WaitForAssertion` waits with no debounce that fail under the same load: B-32.
- The 5 s `DefaultWaitTimeout` (F-48) and the MudBlazor debounce itself; any change under `src/`.
- Making the 24-process stress loop pass 72 of 72: the load stalls processes beyond any fixed ceiling.

## Open questions
- (none)

## Validation script

## Events

- 2026-10-20 question — asked 2, from context 1

## Delivery
- Branch: bug/B-31
