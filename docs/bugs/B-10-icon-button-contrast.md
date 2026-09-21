---
bug: B-10
feature: F-1
status: idea
board: 737
severity: medium
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# Icon buttons below 3:1 contrast

## What happens
Every icon-only button of the kit (row actions such as Edit, Delete and History, the filter chip's remove button) draws its icon in the theme token `ActionDefault`, which is under the 3:1 contrast WCAG 2.2 asks for icons: 1.94:1 in the dark theme (`#404E6A` on the surface `#172035`) and 2.97:1 in the light theme (`#8A96B0` on white). Present since F-1; measured on the kit gallery during the F-14 build (2026-09-21).
1. Open `/dev/ui` in dark mode and look at the row actions of the table → the icons are barely visible.

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
