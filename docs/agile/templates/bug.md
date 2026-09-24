---
bug: B-<number>
feature: F-<number or ->
status: idea
board: <work item id or ->
severity: low | medium | high | critical
---
<!--
One short file per bug. Save as: docs/bugs/B-<number>-<slug>.md
Same status flow as a feature. Approval is needed only when the expected behavior is a product decision.
-->
# <Bug title>

## What happens
<!-- Observed behavior, with the steps to reproduce. -->
1. <Step>

## Start
<!-- What this bug needs before it can be fixed, and how to run it. Filled when the bug is created, confirmed at refinement. -->
- Depends on: <items that must be done first — or "nothing">
- Waits on: <an owner decision, a person or team, access to data or an environment to reproduce it — who provides it — or "nothing">
- Suggested path: <`/agile:refine` → `/agile:build` | `/agile:autopilot <id>` (cause clear, fix small)>
- Parallel with: <ids that can run at the same time in a worktree — or "none">

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
