---
bug: B-22
feature: F-16
status: building
board: 109
severity: low
---
# The data export dialog test fails now and then in the full suite

Technical terms: [glossary](../glossary.md)

## What happens
`Simulab.Web.Tests.Identity.DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne` failed once in the full suite and passes when run alone.

1. In the worktree of F-50 (`feature/F-50`), on 2026-10-03, run `gate.js ship` (the whole solution). Its output saved to a file ended `agile gate RED` with `Failed: 1, Passed: 832, Total: 833` for `Simulab.Web.Tests.dll` and the line `[xUnit.net 00:00:02.84] Simulab.Web.Tests.Identity.DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne [FAIL]`.
2. Run `dotnet test tests/Hosts/Simulab.Web.Tests --filter "FullyQualifiedName~DataExportPageTests"` on the same tree: `Passed: 7, Failed: 0`.
3. The next full run of `gate.js ship` on the same code (after two commits that touch neither the test nor `DataExportPage`) was green: `Simulab.Web.Tests.dll` `Passed: 833`.

It is the kind of failure B-19 measured (a test that loses a race under parallel load). F-50 changes neither the dialog nor its page.

## Start
- Depends on: nothing.
- Waits on (to start): nothing.
- Needed to validate: nothing.
- Suggested path: `/agile:build`. The cause was measured at `/agile:refine` and stays an inference (`## Cause`).
- Parallel with: none.

## Expected
The test passes whenever the solution is tested, alone or in parallel.

## Cause
**Not reproduced.** The failure message of the one red run was not kept, and 30 measured runs of the test at the
refinement (2026-10-04/05, worktree `b-22-data-export-dialog`, branch `bug/B-22` at `8dbe42b` + the item file)
were all green:

| Run | Load | Result |
|---|---|---|
| `dotnet test tests/Hosts/Simulab.Web.Tests --no-build`, 6 in a row | the project alone | 6 green (`Passed: 1044`) |
| `dotnet test Simulab.slnx --no-build`, 4 in a row | the whole solution in parallel | 4 green, no `[FAIL]` line |
| 4 copies of `dotnet test tests/Hosts/Simulab.Web.Tests --no-build` at once, 5 rounds | 4 processes in parallel | 20 green (`Passed!` 20 of 20) |

What the evidence still narrows down:
- The original output (`gate.js ship` in F-50, kept in a session scratch file) has only the `[FAIL]` line and no
  failure message: the gate does not print it. It reported the failure at `[xUnit.net 00:00:02.84]`, the clock of
  `Simulab.Web.Tests` (that project ran 9 s), so less than 3 s after its run started. Every wait of
  the test (`OpenDialog`, the message) is a `WaitForAssertion` with the 5 s timeout of `KitTestContext`
  (`tests/Hosts/Simulab.Web.Tests/Ui/KitTestContext.cs:38`), so none of them could have timed out by then. The
  failure was one of the assertions made **without** waiting.
- The only one of those that depends on a handler having run is the last line:
  `tests/Hosts/Simulab.Web.Tests/Identity/DataExportPageTests.cs:125-126` clicks "Create a password" and reads
  `NavigationManager.Uri` on the next line. This is the pattern B-11 measured (`Click()` sometimes returns before
  the handler runs) and the one the F-8 rule in `.claude/rules/agile/project.md` forbids. F-8 met the same failure
  on a URL read right after `Click()`, seen only in the ship's full suite (`docs/agile/retro-log.md`, F-8 lesson 2).
- A temporary diagnostic (reverted) read the URL right after `Click()`, then waited for it: in all 30 runs the URL was
  already `/forgot-password` right after the click. So the race is real only under a load these runs did not reach;
  it stays an inference, not a reproduction.

The product code is not involved: `PasswordNotSetAlert.razor:21` navigates synchronously in its handler.

Same pattern elsewhere (a click on "Create a password", then `NavigationManager.Uri` asserted on the next line);
each is a test, not a reimplementation of a rule:
- `tests/Hosts/Simulab.Web.Tests/Identity/AccountErasureTests.cs:116-117`
- `tests/Hosts/Simulab.Web.Tests/Identity/ChangePasswordTests.cs:90-91`
- `tests/Hosts/Simulab.Web.Tests/Identity/SecurityPageTests.cs:236-237`

No other test asserts `NavigationManager.Uri` on the line after a `Click()`.

## Fix
Test code only. No production file changes. In each of the four tests, the URL is asserted with
`WaitForAssertion` instead of on the line after `Click()` (rule F-8, as B-11 did):
- `DataExportPageTests.cs:126`, `AccountErasureTests.cs:117`: `dialogs.WaitForAssertion(() => Services.GetRequiredService<NavigationManager>().Uri.Should().EndWith("/forgot-password"))`.
- `ChangePasswordTests.cs:91`, `SecurityPageTests.cs:237`: the same with `page.WaitForAssertion(...)`.
- Found by the first stress loop (change note 2026-10-05, see `## Decisions`): the `disabled` attribute of the
  confirm button read right after `Type(...)` in `AccountErasureTests.Dialog_WithoutAPassword_CannotConfirm`
  (`AccountErasureTests.cs:75`, failed 2 of 40 runs: `Expected ... HasAttribute("disabled") to be False, but found
  True`) and in `DataExportPageTests.cs:79` (same pattern, not yet seen failing). Both assert inside
  `dialogs.WaitForAssertion(...)`.

## Regression test
The four tests themselves. A timing fault that did not reproduce in 30 runs cannot be shown failing on demand,
so the "seen failing before the fix" line of the definition of done is met by the one red run recorded under
`## What happens` (owner, 2026-10-05: fix by the inference).
- `DataExportPageTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne` — `Simulab.Web.Tests`
- `AccountErasureTests.Dialog_AccountWithoutPassword_ShowsHowToCreateOne` — `Simulab.Web.Tests`
- `ChangePasswordTests.Save_AccountWithoutPassword_ShowsHowToCreateOne` — `Simulab.Web.Tests`
- `SecurityPageTests` (the test at line 226) — `Simulab.Web.Tests`
- `AccountErasureTests.Dialog_WithoutAPassword_CannotConfirm` — seen failing 2 of 40 in the first stress loop
  (r4-p2, r7-p2), so this one has a real red run before the fix.
- `DataExportPageTests` (the test at line 70, confirm enabled after typing) — `Simulab.Web.Tests`

Proof of the fix: the stress loop of `## Cause` (4 copies of `dotnet test tests/Hosts/Simulab.Web.Tests --no-build`
at once) for 10 rounds, 40 runs, all `Failed: 0`. The real output is recorded under `## Delivery`.

## Acceptance criteria
- AC1 Given the four tests above, when each one clicks "Create a password", then it asserts the `/forgot-password`
  URL inside `WaitForAssertion`, and no test in `Simulab.Web.Tests` asserts `NavigationManager.Uri` on the line after
  a `Click()`.
- AC1b Given the two tests that type a password and read the confirm button, when each one checks that the button is
  enabled, then it asserts inside `WaitForAssertion`, and no test in `Simulab.Web.Tests` reads a dialog attribute on the
  line after `Type(...)`.
- AC2 Given the fixed tests, when the stress loop runs 10 rounds of 4 parallel runs of `Simulab.Web.Tests`, then all
  40 runs report `Failed: 0`.
- AC3 Given the fix, when the diff is read, then no file under `src/` changed.
- Localization: no UI text changes; the missing-key test stays green.

## Decisions
- 2026-10-05 - Approved by the owner ("aprovo B-22").
- 2026-10-05 - Fix by the inference, without a reproduction - owner; 30 runs did not reproduce it, the change is one
  line per test, it follows a rule the project already has (F-8) and the same fault was measured in B-11 and F-8.
- 2026-10-05 - Fix the three other tests with the same pattern in this bug - owner; same defect, one line each, and it
  avoids three twin bugs later (as in B-11).
- 2026-10-05 - Proof: 10 rounds of the 4-process stress loop - owner; it is the heaviest load measured here and takes
  about 5 minutes, while none of the 4 full-suite runs went red.
- 2026-10-05 - Change note: also fix `AccountErasureTests.cs:75` and `DataExportPageTests.cs:79` (`disabled` read right
  after `Type(...)`) - owner; the valid stress loop had 38 of 40 runs green, and the 2 reds were the first one, so AC2
  cannot pass without it; the second is the same pattern in the class the bug is named after. AC1b added. Affected
  criteria only: AC1b, AC2 (unchanged text, still 40 of 40).
- 2026-10-05 - The first stress loop (copy of the build output outside the worktree) was invalid and is not counted:
  the Stop gate rebuilt the worktree while it ran, and the copy outside the repository fails the 13 stylesheet tests
  ("the tests must run inside the repository"). The proof loop runs on a copy of the build output inside the worktree
  (`bin/`, ignored by git) so the gate cannot rebuild it - technical.
- 2026-10-05 - The gate should keep each failed test's message - owner; it is plugin code, so it went to
  `docs/agile/retro-log.md` as a `plugin` note (⏳) instead of a project idea (memory rule: plugin findings go only in
  the retro-log).
- 2026-10-05 - No new analyzer or architecture test for the pattern - technical; the F-8 rule exists, the occurrences
  are four and found by one grep, and a reliable check for "an assertion after `Click()` that depends on its handler"
  is not simple (same decision as B-11).

## Out of scope
- Any change to `PasswordNotSetAlert.razor` or the dialogs: the product navigates correctly.
- `AppVersionTests` failing in the same F-50 run: that is B-21.
- Changing `gate.js`: recorded as a plugin note (see `## Decisions`).

## Open questions
- (none)

## Validation script
1. Run the stress loop: 4 copies of `dotnet test tests/Hosts/Simulab.Web.Tests --no-build` at once, 10 rounds. → 40 runs, all `Failed: 0`.
2. Run `dotnet test Simulab.slnx` once. → green; the four tests pass.

## Delivery
- Branch: bug/B-22
- Merge: <commit>
