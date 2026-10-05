---
bug: B-23
feature: -
epic: none
status: refining
board: 135
severity: low
---
# Two tests read the URL right after a click, before the handler may have run

Technical terms: [glossary](../glossary.md)

## What happens
Two tests in `Simulab.Web.Tests` click a button and assert `NavigationManager.Uri` on the next statement, with only a
blank line between, without `WaitForAssertion`. `Click()` can return before the handler has run (the race measured in
B-11 and B-22; rule F-8 in `.claude/rules/agile/project.md`), so they can fail now and then under load.

1. `tests/Hosts/Simulab.Web.Tests/Identity/ResetPasswordTests.cs:97-99`: `Submit_Success_GoesToSignInWithTheAlertAndNoPersonalData`
   clicks `button.app-reset-password-submit`, then asserts the URL ends with `ResetPassword.SignInAfterResetPath`.
2. `tests/Hosts/Simulab.Web.Tests/Catalog/ExamEditionsSectionTests.cs:167-169`: clicks the edit button of an edition
   (`button[aria-label='Edit: 2026, CEBRASPE']`), then asserts `NavigationManager.Uri`.

Neither has been seen failing. They were found by the grep run for B-22 (a `Click()` followed by a
`NavigationManager.Uri` read), and left out of B-22 because its AC1 says "on the line after".

## Start
- Depends on: nothing. Related: B-22 (same defect, four other tests; in validating when this was captured).
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:refine` -> `/agile:build` (or `/agile:autopilot <id>`): test code only, one line per test,
  assert inside `WaitForAssertion`.
- Parallel with: none.
- Cause not verified: measure it at `/agile:refine`. The symptom seen is none; the two lines were found by reading.

## Expected
Both tests pass whenever the solution is tested, alone or in parallel.

## Cause
**Not reproduced.** Measured at the refinement (2026-10-05, worktree `b-23-uri-read-after-click`, branch `bug/B-23`
at `f06135f`), with the stress loop of the project rule (copy of the build output in `bin/stress`, git-ignored):

| Run | Load | Result |
|---|---|---|
| 4 copies of `dotnet test Simulab.Web.Tests.dll` at once, 5 rounds | 4 processes in parallel | 20 of 20 `Passed!  - Failed: 0, Passed: 1044` |

What the code says about each of the two tests:
- `ResetPasswordTests.cs:97-99`: the handler awaits before it navigates.
  `src/Hosts/Simulab.Web/Components/Pages/Identity/ResetPassword.razor:215` awaits `Api.ResetPasswordAsync` (an
  `HttpClient` call to the `StubApiHandler` of `IdentityPageTestContext`), then navigates at line 220. This is the
  case rule F-8 names ("a click whose handler awaits"); in these runs the stub answered fast enough for the URL to be
  set before `Click()` returned, so the race stays an inference.
- `ExamEditionsSectionTests.cs:167-169`: the handler is synchronous.
  `src/Hosts/Simulab.Web/Components/Pages/Catalog/ExamEditionsSection.razor:160-161` navigates without an await,
  through `AppRowActions` (`OnEdit.InvokeAsync()`). By rule F-8 it is not a race today; it becomes one the day the
  handler awaits anything.

The product code is not involved in either test.

Same pattern elsewhere: a `Click()` followed by a `NavigationManager.Uri` assertion, not inside `WaitForAssertion`
(all synchronous handlers today; each is a test, not a reimplementation of a rule):
- `tests/Hosts/Simulab.Web.Tests/Admin/AccountEventsPageTests.cs:162-165` (handler `Users.razor:157-160`)
- `tests/Hosts/Simulab.Web.Tests/Admin/RoleHistoryPageTests.cs:164-166` (handler `Roles.razor:124-127`)
- `tests/Hosts/Simulab.Web.Tests/Admin/RoleHistoryPageTests.cs:172-174` (handler `Users.razor:149-152`)
- `tests/Hosts/Simulab.Web.Tests/Identity/GoogleSignInPageTests.cs:52-54` and `:63-65` (`AppGoogleButton`)
- `tests/Hosts/Simulab.Web.Tests/Identity/GoogleSignUpPageTests.cs:169-170` and `:192-194` (`GoogleSignUp.razor:237`
  and the cancel button)

URL reads not preceded by a click (after `Render`, after a `WaitForAssertion`, or negative `NotContain` checks) are
not this pattern and are left out.

## Fix
Not decided yet.

## Regression test
- Not decided yet.

## Open questions
- (none)

## Validation script
1. Not written yet.

## Delivery
- Branch: bug/B-23
- Merge: <commit>
