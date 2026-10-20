---
bug: B-32
feature: -
status: refining
board: 200
severity: low
epic: Test infrastructure
---
# Plain WaitForAssertion waits in bUnit page tests time out under heavy load

Technical terms: [glossary](../glossary.md)

## What happens
bUnit page tests that wait with a bare `WaitForAssertion` fail when the machine is starved for seconds, because the wait ends at the default ceiling of 5 s (`KitTestContext.DefaultWaitTimeout`, `tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:46`) while the work they wait for is still on its way. Found while refining B-31 (2026-10-20): 24 processes of `dotnet test --filter "FullyQualifiedName~QuestionBank"` at once on copies of the build output.
1. `Simulab.Web.Tests.QuestionBank.QuestionOriginTests.Save_WithAnOrigin_SendsTheEditionTheNumberAndTheNoticeSubject` failed 2 times in 48 processes (15 s each), a test with no debounced field, so the helper of B-30 and B-31 does not cover it.
2. The load measured in `docs/bugs/B-31-base-texts-search-test-flaky.md` (`## Cause`) failed 44 real tests of the area in 48 processes, most of them debounced waits (B-31's scope) and the rest waits like the one above.

Already decided, so a later refinement does not read it as open: F-48 (2026-10-03) keeps the 5 s default ceiling ("a ceiling, a green run never waits on it"). A general ceiling would change that decision, so it needs the owner's answer at refinement. Already fixed by earlier bugs: B-19 (thread-pool minimum), B-22, B-23, B-30 (a debounced wait whose clock advance came before the delay existed) and B-31 (the debounced wait's own ceiling). What is left: the plain waits.

## Start
- Depends on: B-31 (done, merged 2026-10-20 as `2221539`; it measures the load and adds the helper's own ceiling).
- Waits on (to start): nothing.
- Needed to validate: nothing; no screen. The proof is the settings and wait tests of the regression test, and a stress run like the one in `docs/bugs/B-31-base-texts-search-test-flaky.md`.
- Model: sonnet (no signal)
- Suggested path: `/agile:build B-32`.
- Parallel with: F-89 (its worktree touches no test helper; a conflict on `KitTestContext.cs` is possible but small).

## Expected
A bUnit test that waits with a plain `WaitForAssertion`, `WaitForState` or `WaitForElement` gets its result even when the machine stalls the test process for seconds. The shared ceiling of those waits is 15 s, the same as the debounced waits already have (B-31). A wait whose assertion passes returns at once; only a failing test pays the longer ceiling. Nothing under `src/` changes.

- AC1 Given the shared test context, when its settings are read, then `BunitContext.DefaultWaitTimeout` is 15 s and `DebounceWaitTimeout` is 15 s.
- AC2 Given a plain `WaitForAssertion` with no timeout argument whose assertion becomes true only after 6 s of wall time, when the test waits, then the wait passes (with 5 s it throws `WaitForFailedException`).
- AC3 Given the plain waits that pass an explicit 5 s (`BaseTextsPageTests.cs:213`, `QuestionsPageTests.cs:235`), when the item is built, then they pass no timeout and use the shared ceiling; no test source passes a 5 s timeout to a wait.
- AC4 Given the change, when `Simulab.Web.Tests` runs, then every test passes with the same assertions (none is weakened, removed or skipped), including the six tests of `DebounceWaitTimeoutTests` with the settings test updated to 15 s.
- AC5 Given 24 processes of `dotnet test --filter "FullyQualifiedName~QuestionBank"` at once on copies of the build output, 3 rounds, when they finish, then the result is recorded as measured, with the shape of each red (baselines of 2026-10-20: B-31 run 2 failed `QuestionOriginTests.Save_WithAnOrigin_SendsTheEditionTheNumberAndTheNoticeSubject` 2 times in 48 processes). The loop is weak evidence only and does not gate the ship: its load is artificial; AC1 and AC2 are the proof.
- AC6 No file under `src/` changes and no package is added. No UI text: the localization criterion does not apply.

## Cause
Measured in B-31 (`## Cause` of `docs/bugs/B-31-base-texts-search-test-flaky.md`): process-wide starvation under 24 parallel test processes, not a lost event; stalls of up to 9.7 s between an action and the first check of its wait, and 20 s still lost 1 of 720 runs under a heavier load.

Checked in today's code (2026-10-20, `main` at `f43f470`, after B-31): every plain wait (`WaitForAssertion`, `WaitForState`, `WaitForElement` with no timeout argument) ends at `BunitContext.DefaultWaitTimeout`, which `KitTestContext` sets to 5 s in its constructor (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:46`). The value is static (bUnit 2.10.3), so it is one number for the whole test process. There are about 1140 such calls in 83 files; only two pass a timeout of their own (`BaseTextsPageTests.cs:213`, `QuestionsPageTests.cs:235`, both 5 s). The debounced waits already have their own 15 s ceiling (B-31, `KitTestContext.cs:59`); the plain ones did not get it, so a process stalled for more than 5 s fails the test for the machine's load and not for the code (`QuestionOriginTests.Save_WithAnOrigin_SendsTheEditionTheNumberAndTheNoticeSubject`, `QuestionOriginTests.cs:173`, 2 failures in 48 processes).

Duplicates: none that is a business rule (this is test timing). The same number is also asserted by `DebounceWaitTimeoutTests.SharedContext_Settings_DebounceWaitIs15SecondsAndDefaultWaitStays5` (`DebounceWaitTimeoutTests.cs:89`), which a new ceiling must update.

Callers: `KitTestContext` constructor (`KitTestContext.cs:46`) is the only place that sets the shared value; the ~1140 waits are `same defect` and none relies on 5 s as a limit (a green wait returns as soon as its assertion passes).

## Fix
- `KitTestContext` (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:46`): `DefaultWaitTimeout = TimeSpan.FromSeconds(15)`. The comment above it (B-19) says why it is 15 s now: the stalls measured in B-31 reached 9.7 s, and a green run never waits on a ceiling.
- `DebounceWaitTimeout` keeps its own property (a test can still shrink it to prove the helper's ceiling); its comment no longer says it is longer than the default wait.
- `BaseTextsPageTests.cs:213` and `QuestionsPageTests.cs:235`: drop the explicit `TimeSpan.FromSeconds(5)` so both use the shared ceiling.
- `DebounceWaitTimeoutTests.SharedContext_Settings_DebounceWaitIs15SecondsAndDefaultWaitStays5` (`DebounceWaitTimeoutTests.cs:89`): renamed and updated to AC1.
- Nothing else: the ~1140 waits need no edit.

## Docs
- none — test-only change; no technical page describes the wait ceiling.

## Regression test
- AC1 and AC2 in `DebounceWaitTimeoutTests` (same folder and collection `SerialDebounceWait` as B-31's, because `DefaultWaitTimeout` is static): AC1 reads the settings; AC2 waits for an assertion that turns true after 6 s of wall time (costs about 6 s once). Both must be seen failing before the fix.

## Decisions
- 2026-10-20 — Captured as a separate bug and not as part of B-31 — the owner kept B-31 to the debounced helper and put the plain waits in a later item; the epic "Test infrastructure" is new — asked
- 2026-10-20 — The 5 s default ceiling of F-48 (2026-10-03: "a ceiling, a green run never waits on it") is changed — No, change it; the same reasoning holds for 15 s and the measured stalls (up to 9.7 s) exceed 5 s — changed: 5 s default ceiling (docs/features/F-48-debounce-on-timeprovider.md)
- 2026-10-20 — The new shared ceiling is 15 s, one place for every plain wait, rather than 10 s, 20 s, a ceiling per kind of wait or accepting the risk — it equals the debounced helper's ceiling (B-31), covers the measured stalls with margin, and a failing test pays the extra wait — asked
- 2026-10-20 — AC2 uses a real 6 s wall-clock wait, accepting about 6 s once in `Simulab.Web.Tests` — a smaller proof would need a non-static ceiling, which bUnit 2.10.3 does not offer — decided by Claude
- 2026-10-20 — The stress loop (AC5) stays a recorded measurement, not a gate, as in B-31 — decided by Claude
- 2026-10-20 — Main-session model: sonnet (no signal) — `references/model-choice.md` "Main session"
- 2026-10-20 — Reuse: `KitTestContext` and B-31's `DebounceWaitTimeoutTests` and `SerialDebounceWait` extended in place; no new package — decided by Claude

## Out of scope
- The 15 s debounced helper (B-31) and the MudBlazor debounce itself; any change under `src/`.
- Making the 24-process stress loop pass 72 of 72: a load that stalls a process beyond any fixed ceiling cannot be fixed by a ceiling.
- Test projects other than `Simulab.Web.Tests` that do not use `KitTestContext`.

## Open questions
- (none)

## Validation script

## Events

- 2026-10-20 question — asked 2, from context 0

## Delivery
- Branch: bug/B-32
