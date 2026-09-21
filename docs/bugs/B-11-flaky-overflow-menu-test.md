---
bug: B-11
feature: F-1
status: idea
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
<!-- Correct behavior. If this is a product decision, it goes to the owner. -->

## Cause
<!-- Confirmed in code: file and line. Never a guess. -->
<!-- If this is a business rule, list every duplicate occurrence found elsewhere in the solution (file:line each), or write "no duplicate found". -->

## Fix
<!-- What changes. Keep it minimal. -->
<!-- If duplicates were listed above: which are fixed here, which are deferred and why. -->

## Regression test
<!-- The test that fails before the fix and passes after it. One per occurrence fixed. -->
- <Test name> — <project>

## Open questions
- (none)

## Validation script
1. <Step> → <expected result>

## Delivery
- Branch: <bug/B-<number>>
- Merge: <commit>
