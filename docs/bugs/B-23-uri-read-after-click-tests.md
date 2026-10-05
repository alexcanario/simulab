---
bug: B-23
feature: -
epic: none
status: validating
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
- Cause measured at `/agile:refine` (2026-10-05): not reproduced in 20 parallel runs; it stays an inference
  (`## Cause`).

## Expected
The nine tests listed under `## Cause` (the two above and the seven of "Same pattern elsewhere") pass whenever the
solution is tested, alone or in parallel, and no test in the solution reads `NavigationManager.Uri` on the statement
after a `Click()` again.

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
Test code only. No production file changes.
- In each of the nine tests of `## Cause`, the URL assertion after `Click()` moves inside `WaitForAssertion` on the
  rendered component the test already holds (`page`, `section`, `users`, `roles`), as B-22 did:
  `x.WaitForAssertion(() => <NavigationManager>.Uri.Should().EndWith(...))`. Nothing else in those tests changes.
- A guard test, `tests/Simulab.ArchitectureTests/UiTestTimingTests.cs`, reads every `*Tests.cs` file under `tests/`
  and fails when a statement ending in `.Click();` is followed, after blank lines only, by a statement that reads
  `.Uri` outside a `WaitForAssertion(` call. The failure lists each hit as `file:line`. It is a source-text check, the
  same kind as the other repository-reading tests of that project.

## Regression test
- `UiTestTimingTests.ClickThenUriRead_InTestSources_IsNotFound`: run before the fix, it fails and lists the nine
  locations of `## Cause` (that failing output is recorded under `## Delivery`); after the fix it passes.
- Stress loop of the project rule, on a copy of the build output under `bin/`: 4 copies of
  `dotnet test Simulab.Web.Tests.dll` at once, 10 rounds, 40 runs, every one `Failed: 0`. The real output is recorded
  under `## Delivery`.

## Acceptance criteria
- AC1 Given the nine tests of `## Cause`, when each one clicks the control that navigates, then it asserts the URL
  inside `WaitForAssertion`, never on the statement after `Click()`.
  -> the nine tests themselves, green; `UiTestTimingTests.ClickThenUriRead_InTestSources_IsNotFound`.
- AC2 Given a test source with a `Click()` followed (blank lines only) by a `.Uri` read outside `WaitForAssertion`,
  when the architecture tests run, then the guard fails and names that file and line; with none, it passes.
  -> `UiTestTimingTests.ClickThenUriRead_InTestSources_IsNotFound`, seen failing before the fix with the nine hits.
- AC3 Given the fixed tests, when `Simulab.Web.Tests` runs in 4 parallel processes for 10 rounds, then all 40 runs
  report `Failed: 0`. -> stress loop output under `## Delivery`.
- Localization: not applicable; no UI text changes.

## Decisions
- 2026-10-05 Similar item: B-22 (done) is the same defect in six other tests; keep both. B-22 did not touch these
  tests and recorded them as this item (owner, refinement card).
- 2026-10-05 Scope: all nine tests of `## Cause`, not only the two of the title. One line per test, and it removes the
  pattern from the solution in one sweep, as the B-22 retro (lesson 2) asked (owner, refinement card).
- 2026-10-05 Proof: a guard test that reads the test sources plus the stress loop. No failure was ever seen, so the
  guard is the regression test that can be seen failing before the fix (definition of done) and it keeps the pattern
  from coming back (owner, refinement card).
- 2026-10-05 Guard placement and name: `Simulab.ArchitectureTests`, where the other repository-reading convention
  tests live; class `UiTestTimingTests`. Technical choice, no product impact.
- 2026-10-05 Guard reach: only `Click()` then a `.Uri` read. The wider family (a `Type()`/`Change()` then a markup or
  attribute read) is not checked: those reads are legitimate after a synchronous handler and a text rule cannot tell
  which handlers await. Technical choice, no product impact.
- 2026-10-05 Packages: none.
- 2026-10-05 Build: the guard found a tenth hit the refinement grep missed, `SignInProfileTests.cs:34`
  (`SignIn_IssuesATicketWithTheAccountNameAndLanguage`, handler awaits). Fixed in the same sweep, so "the nine tests"
  of the criteria are ten; the guard, not the list, is the proof that none is left.

## Out of scope
- Product code: no handler changes.
- Reads after `Type()`, `Change()` or a fill that are not a URL read (see the guard reach decision).
- URL reads not preceded by a click (after `Render`, after a `WaitForAssertion`, `NotContain` checks).

## Open questions
- (none)

## Validation script
No screen changes. The owner checks the evidence under `## Delivery`:
1. The guard's failing output before the fix lists the ten locations (the nine of `## Cause` plus `SignInProfileTests.cs:34`).
2. The guard passes after the fix (`Simulab.ArchitectureTests` green).
3. The stress loop shows 40 of 40 runs with `Failed: 0`.

## Delivery
- Branch: bug/B-23
- Merge: <commit>
- Guard before the fix (`UiTestTimingTests.ClickThenUriRead_InTestSources_IsNotFound`, `Failed: 1, Passed: 2`), hits:
  `AccountEventsPageTests.cs:165`; `RoleHistoryPageTests.cs:166`, `:174`; `ExamEditionsSectionTests.cs:169`;
  `GoogleSignInPageTests.cs:54`, `:65`; `GoogleSignUpPageTests.cs:170`, `:194`; `ResetPasswordTests.cs:99`;
  `SignInProfileTests.cs:34`.
- After the fix: `Simulab.ArchitectureTests` `Passed!  - Failed: 0, Passed: 167`; `Simulab.Web.Tests` `Passed!  - Failed: 0,
  Passed: 1044`; `agile gate GREEN`.
- Stress loop (4 parallel `dotnet test Simulab.Web.Tests.dll` on a copy of the build output, 10 rounds): 40 files,
  40 lines `Passed!  - Failed: 0, Passed: 1044`, 0 `Failed!`.
- Criterion -> test: AC1 -> the ten tests, green, and the guard; AC2 -> the guard (seen failing, then passing) and its
  two negative controls (`FindClickThenUriRead_UriReadAfterAClick_NamesTheFileAndLine`,
  `FindClickThenUriRead_UriReadInsideWaitForAssertion_IsLeftAlone`); AC3 -> the stress loop above.
