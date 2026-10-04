---
bug: B-23
feature: -
epic: none
status: idea
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
Not investigated yet.

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
