---
bug: B-32
feature: -
status: idea
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
- Depends on: B-31 (it measures the load and adds the helper's own ceiling; the work starts after its merge).
- Waits on (to start): nothing.
- Needed to validate: nothing; no screen. The proof is a stress run like the one in `docs/bugs/B-31-base-texts-search-test-flaky.md`.
- Model: unknown — settled at `/agile:refine`
- Suggested path: `/agile:refine B-32`, then `/agile:build B-32`. Cause measured in B-31 (starvation); the open question is the decision: a general ceiling, a ceiling per kind of wait, or accepting the risk.
- Parallel with: unknown — settled at `/agile:refine`

## Expected
unknown — settled at `/agile:refine`

## Cause
Measured in B-31 (`## Cause` of `docs/bugs/B-31-base-texts-search-test-flaky.md`): process-wide starvation under 24 parallel test processes, not a lost event. Not measured here for the plain waits.

## Fix
unknown — settled at `/agile:refine`

## Docs
- none — test-only, to be confirmed at `/agile:refine`

## Regression test
- unknown — settled at `/agile:refine`

## Decisions
- 2026-10-20 — Captured as a separate bug and not as part of B-31 — the owner kept B-31 to the debounced helper and put the plain waits in a later item; the epic "Test infrastructure" is new — asked

## Open questions
- (none)

## Validation script

## Events

## Delivery
- Branch: bug/B-32
