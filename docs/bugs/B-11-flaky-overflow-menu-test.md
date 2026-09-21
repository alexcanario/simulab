---
bug: B-11
feature: F-1
status: refining
board: 739
severity: low
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Flaky overflow menu test in AppRowActionsTests

## What happens
`AppRowActionsTests.Render_FiveActions_ShowsThreeAndPutsTheRestInOverflowMenu` (`tests/Hosts/Simulab.Web.Tests`)
failed on the first run after each build, both with the B-10 change and on the `main` code alone, with
`Expected clicked to be equal to {"Archive"}, but found empty collection.`; it passed on every rerun. Found during
the B-10 build (2026-09-21). The test asserts the overflow menu item's click on the line after `Click()`, not with
`WaitForAssertion` (project rule from F-8).
1. Build `Simulab.Web.Tests`, then run `dotnet test tests/Hosts/Simulab.Web.Tests` → sometimes 1 failure on the
   first run; `--no-build` reruns pass.

## Expected
The Web test suite gives the same result on every run: a test that clicks a popover or menu item waits for the
effect with `WaitForAssertion` before asserting it. The product itself is unchanged.

## Cause
The test, not the component. `tests/Hosts/Simulab.Web.Tests/Ui/AppRowActionsTests.cs:69-70` clicks the overflow item
and asserts `clicked` on the next line. Sometimes `Click()` returns before the item's handler has run, and the handler
runs a moment later.

Reproduced on `main` at `7ddc221` (2026-09-21), with a clean build (`--no-incremental`) before each run of
`dotnet test tests/Hosts/Simulab.Web.Tests`:
- unchanged test: failed 2 times in 3 runs with `Expected clicked to be equal to {"Archive"}, but found empty collection.`;
- temporary diagnostic (count read right after `Click()`, then `WaitForAssertion` on `clicked`, then assert the
  count): 3 runs in 8 failed with `Expected countAfterClick to be 1 ... but found 0`. In every run the
  `WaitForAssertion` passed, so the handler always ran, just after `Click()` returned. The diagnostic was reverted.

The component is not at fault. In MudBlazor 9.9.0, `MudMenuItem.OnClickHandlerAsync` calls the item's `OnClick`
before it closes the menu (`src/MudBlazor/Components/Menu/MudMenuItem.razor.cs:146-168`, tag `v9.9.0`), and
`AppRowActions.razor` passes the action straight through. The only thing that varies is when bUnit runs the handler
while the popover is still rendering. The project rule from F-8 already covers this case: after a click whose handler
awaits, assert with `WaitForAssertion`.

Other occurrences, checked in every test file that renders `MudPopoverProvider`:
- `tests/Hosts/Simulab.Web.Tests/Layout/UserMenuTests.cs:42-44` has the same pattern on opening a menu: it clicks the
  user menu button and reads `popovers.Find(".app-user-menu-account")` on the next line. It has not been seen failing.
- Every other click on a dialog, table or popover element is followed by `WaitForAssertion`
  (`AccountErasureTests`, `AppDataTableTests`, the open step of `AppRowActionsTests`). No other occurrence found.

## Fix
Test code only. No production file changes.
- `AppRowActionsTests.cs:70`: `popovers.WaitForAssertion(() => clicked.Should().Equal("Archive"))`.
- `UserMenuTests.cs:44`: wait for the menu item with `popovers.WaitForAssertion(...)` before reading it (owner, 2026-09-21: fix both in this bug).

## Regression test
The tests themselves are the regression tests. A timing fault cannot be made to fail on demand, so the failing run is
the reproduction recorded under `## Cause` (2 in 3, then 3 in 8).
- `AppRowActionsTests.Render_FiveActions_ShowsThreeAndPutsTheRestInOverflowMenu` — `Simulab.Web.Tests`
- `UserMenuTests` (the test at line 42) — `Simulab.Web.Tests`; never seen failing, fixed for the same pattern.

Proof of the fix: the loop that reproduced the fault (a clean build with `--no-incremental`, then
`dotnet test tests/Hosts/Simulab.Web.Tests --no-build`) gives 10 green runs in 10. The real output is recorded here.

## Acceptance criteria
- AC1 Given the fixed `AppRowActionsTests`, when the reproduction loop runs 10 times after a clean build each time, then all 10 runs report `Failed: 0`.
- AC2 Given the fixed `UserMenuTests`, when the same loop runs, then it never fails, and it waits for the popover item with `WaitForAssertion`.
- Localization: no UI text changes.

## Decisions
- 2026-09-21 - Fix `UserMenuTests.cs:42-44` in this bug - owner; same defect, one line, and it avoids a twin bug later.
- 2026-09-21 - Prove the fix with 10 clean-build runs in a row - owner; the fault showed in about 35% of runs, so one green gate could pass by luck.
- 2026-09-21 - No new build check for the pattern - technical; the rule already exists (F-8), and a reliable analyzer for "an assert after `Click()` on popover content" is not simple. The two occurrences are the only ones in the suite.

## Out of scope
- Any change to `AppRowActions.razor` or to MudBlazor usage: the component behaves correctly.

## Open questions
- (none)

## Validation script
1. `dotnet build tests/Hosts/Simulab.Web.Tests --no-incremental`, then `dotnet test tests/Hosts/Simulab.Web.Tests --no-build` → `Failed: 0, Passed: 328`. Repeat 3 times; every run is green.

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
